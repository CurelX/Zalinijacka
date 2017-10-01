using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoutineManager : MonoBehaviour
{
	private static RoutineManager _core;

	public static RoutineManager Core {
		get {
			if (_core == null) {
				GameObject go = new GameObject ();
				go.name = "[RoutineManager]";

				_core = go.AddComponent<RoutineManager> ();
			}

			return _core;
		}
	}

	Routine first = null;
	int currentFrame;
	float currentTime;

	private Routine coroutine;
	private Routine listNext;
	private IEnumerator fiber;

	#region Pool
//	List<Routine> freePool = new List<Routine>();
//	List<Routine> activePool = new List<Routine>();
//
//	void Awake()
//	{
//		for (int i = 0; i < 15; i++)
//			freePool.Add(new Routine());
//
//	}
//
//	Routine Grab()
//	{
//		Routine coroutine = null;
//		if (freePool.Count == 0) {
//			coroutine = new Routine();
//			activePool.Add(coroutine);
//			return coroutine;
//		}
//
//		int lastIndex = freePool.Count - 1;			
//		coroutine = freePool[lastIndex];
//		activePool.Add(coroutine);
//		freePool.RemoveAt(lastIndex);
//		coroutine.Reset();
//		return coroutine;
//	}
//
//	void Free(Routine coroutine)
//	{
//		activePool.Remove(coroutine);
//		freePool.Add(coroutine);
//	}
//
//	public string PoolReport()
//	{
//		return "dict " + activePool.Count + "   list " + freePool.Count;
//	}

	#endregion

	void Update ()
	{
		updateAllRoutines (Time.frameCount, Time.time);
		updateAllSequenceRoutines();
	}

	//Starts a coroutine, the coroutine does not run immediately but on the
	//next call to UpdateAllCoroutines. The execution of a coroutine can
	//be paused at any point using the yield statement. The yield return value
	//specifies when the coroutine is resumed.
	// mark any coroutine that needs to be repeated as IEnumerable
	// so that you can be able to use replay method
	// this is because IEnumeratros don't allow multiple copies and
	// can't be copied as they are passed by reference so we must use
	// GetEnumerator() method of IEnumerable to be able to create this
	// effect and create new IEnumerator at whim when replaying coroutine

	/// <summary>
	/// Starts a coroutine. Coroutine will not run immediatly but will be called on the 
	/// next update of game loop. IEnumerable coroutines can be replayed, since we can always
	/// get the IEnumerator via IEnumerable.
	/// </summary>
	/// <returns>Returns the Routine object which stores the reference to the coroutine to be used later.</returns>
	/// <param name="fiberSource">IEnumerable that supplies the coroutine.</param>
	public Routine startRoutine (IEnumerable fiberSource)
	{
		// if function does not have a yield, fiber will be null and we no-op
		if (fiberSource == null) {
			return null;
		}
		// create coroutine node and run until we reach first yield

		Routine coroutine = new Routine (fiberSource);
//		Routine coroutine = Grab().SetTo(fiberSource);
		addRoutine (coroutine);
		return coroutine;
	}

	//starts the coroutine performing it's first update immediatly
	/// <summary>
	/// Same as startCoroutine but will perform the first update immediatly.
	/// </summary>
	/// <returns>Returns the Routine object which stores the reference to the coroutine to be used later.</returns>
	/// <param name="fiberSource">IEnumerable that supplies the coroutine.</param>
	public Routine startRoutineAsap (IEnumerable fiberSource)
	{
		// if function does not have a yield, fiber will be null and we no-op
		if (fiberSource == null) {
			return null;
		}
		// create coroutine node and run until we reach first yield
		Routine coroutine = new Routine (fiberSource);
//		Routine coroutine = Grab().SetTo(fiberSource);
		addRoutine (coroutine);
		updateRoutine(coroutine);
		return coroutine;
	}

	/**
   * Stops all coroutines running on this behaviour. Use of this method is
   * discouraged, think of a natural way for your coroutines to finish
   * on their own instead of being forcefully stopped before they finish.
   * If you need finer control over stopping coroutines you can use multiple
   * schedulers.
   */
	public void stopAllRoutines ()
	{
		first = null;

	}

	/**
   * Returns true if this scheduler has any coroutines. You can use this to
   * check if all coroutines have finished or been stopped.
   */
	public bool hasRoutines ()
	{
		return first != null;
	}

	/**
   * Runs all active coroutines until their next yield. Caller must provide
   * the current frame and time. This allows for schedulers to run under
   * frame and time regimes other than the Unity's main game loop.
   */
	public void updateAllRoutines (int frame, float time)
	{
		currentFrame = frame;
		currentTime = time;
		coroutine = this.first;
		while (coroutine != null) {
			// store listNext before coroutine finishes and is removed from the list
			listNext = coroutine.listNext;

			if (coroutine.waitForFrame > 0 && frame >= coroutine.waitForFrame) {
				coroutine.waitForFrame = -1;
				updateRoutine (coroutine);
			} else if (coroutine.waitForTime > 0.0f && time >= coroutine.waitForTime) {
				coroutine.waitForTime = -1.0f;
				updateRoutine (coroutine);
			} else if (coroutine.waitForCoroutine != null && coroutine.waitForCoroutine.finished) {
				coroutine.waitForCoroutine = null;
				updateRoutine (coroutine);
			} else if (coroutine.waitForBool == true) {
				coroutine.waitForBool = false;
				updateRoutine (coroutine);
			} else if (coroutine.waitForFrame == -1 && coroutine.waitForTime == -1.0f && coroutine.waitForCoroutine == null) {
				// initial update
				updateRoutine (coroutine);
			}
			coroutine = listNext;
		}
	}


	/**
   * Executes coroutine until next yield. If coroutine has finished, flags
   * it as finished and removes it from scheduler list.
   */
	void updateRoutine (Routine coroutine)
	{
		if (coroutine.paused)
			return;

		fiber = coroutine.fiberEnum;

		if (coroutine.fiberEnum.MoveNext ()) {
			System.Object yieldCommand = fiber.Current == null ? (System.Object)1 : fiber.Current;

			if (yieldCommand.GetType () == typeof(int)) {
				coroutine.waitForFrame = (int)yieldCommand;
				coroutine.waitForFrame += (int)currentFrame;
			} else if (yieldCommand.GetType () == typeof(float)) {
				coroutine.waitForTime = (float)yieldCommand;
				coroutine.waitForTime += (float)currentTime;
			} else if (yieldCommand.GetType () == typeof(Routine)) {
				coroutine.waitForCoroutine = (Routine)yieldCommand;
			} else if (yieldCommand.GetType () == typeof(bool)) {
				coroutine.waitForBool = (bool)yieldCommand;
			} else {
				throw new System.ArgumentException ("RoutineManager: Unexpected coroutine yield type: " + yieldCommand.GetType ());
			}
		} else {
			// coroutine finished
			coroutine.finished = true;
			destroyRoutine (coroutine);
		}
	}

	public void addRoutine (Routine coroutine)
	{

		if (this.first != null) {
			coroutine.listNext = this.first;
			first.listPrevious = coroutine;
		}
		first = coroutine;
	}

	public void destroyRoutine (Routine coroutine)
	{
		if (this.first == coroutine) {
			// remove first
			this.first = coroutine.listNext;
		} else {
			// not head of list
			if (coroutine.listNext != null) {
				// remove between
				coroutine.listPrevious.listNext = coroutine.listNext;
				coroutine.listNext.listPrevious = coroutine.listPrevious;
			} else if (coroutine.listPrevious != null) {
				// and listNext is null
				coroutine.listPrevious.listNext = null;
				// remove last
			}
		}
		coroutine.listPrevious = null;
		coroutine.listNext = null;
	}

	/*Flushes the coroutine executing all of the code untill the end instantly.
	 * */
	public void flushRoutine (Routine coroutine)
	{
		while (coroutine.fiberEnum.MoveNext ()) {
		}
		coroutine.finished = true;
		destroyRoutine (coroutine);
	}

	//===================//
	//Custom methods     //
	//===================//
	public void pauseRoutine (Routine coroutine)
	{
		coroutine.paused = true;
	}

	public void resumeRoutine (Routine coroutine)
	{
		coroutine.paused = false;
	}

	public void replayRoutine (ref Routine coroutine)
	{
		if (coroutine.fiberSource != null) {
			Routine cn =  new Routine (coroutine.fiberSource);
//			Routine cn = Grab().SetTo(coroutine.fiberSource);
			destroyRoutine (coroutine);
			coroutine = startRoutine (cn.fiberSource);
		}
	}

	#region CoroutineSequencer

	RoutineSequence routineSequence;
	RoutineSequence sequenceHead;
	RoutineSequence sequenceNext;

	public RoutineSequence startRoutineSequence (IEnumerable[] fibers)
	{
		// if function does not have a yield, fiber will be null and we no-op
		if (fibers == null || fibers.Length == 0) {
			return null;
		}
		// create coroutine node and run until we reach first yield
		RoutineSequence sequence = new RoutineSequence(fibers);
		addSequence(sequence);
		sequence.currentCoroutine = startRoutine(fibers[++sequence.currentFiber]);

		//		Debug.LogError("1"  + sequence.currentFiber);
		return sequence;
	}

	public void updateAllSequenceRoutines()
	{
		routineSequence = this.sequenceHead;
		while (routineSequence != null) {
			// store listNext before coroutine finishes and is removed from the list
			sequenceNext = routineSequence.listNext;


			if (routineSequence.currentFiber >= routineSequence.fibers.Length && routineSequence.currentCoroutine.finished) {
				destroyRoutineSequence(routineSequence);
			}
			else if (routineSequence.currentCoroutine.finished) {
				routineSequence.currentFiber++;
				if (routineSequence.currentFiber < routineSequence.fibers.Length) {
					routineSequence.currentCoroutine = startRoutine(routineSequence.fibers[routineSequence.currentFiber]);	
				}
			}

			routineSequence = sequenceNext;
		}
	}

	public void addSequence (RoutineSequence sequence)
	{

		if (this.sequenceHead != null) {
			sequence.listNext = this.sequenceHead;
			sequenceHead.listPrevious = sequence;
		}
		sequenceHead = sequence;
	}

	public void destroyRoutineSequence (RoutineSequence sequence)
	{
		if (this.sequenceHead == sequence) {
			// remove first
			this.sequenceHead = sequence.listNext;
		} else {
			// not head of list
			if (sequence.listNext != null) {
				// remove between
				sequence.listPrevious.listNext = sequence.listNext;
				sequence.listNext.listPrevious = sequence.listPrevious;
			} else if (sequence.listPrevious != null) {
				// and listNext is null
				sequence.listPrevious.listNext = null;
				// remove last
			}
		}
		sequence.listPrevious = null;
		sequence.listNext = null;

		if (sequence.currentCoroutine == null) {
			destroyRoutine(sequence.currentCoroutine);
			sequence.currentCoroutine = null;
		}
	}

	public void flushRoutineSequence(RoutineSequence sequence)
	{
		if (sequence.currentCoroutine.finished != null)
			flushRoutine(sequence.currentCoroutine);

		for (int i = sequence.currentFiber + 1; i < sequence.fibers.Length; i++) {
			sequence.currentCoroutine = startRoutine(sequence.fibers[i]);
			flushRoutine(sequence.currentCoroutine);
		}
		destroyRoutineSequence(sequence);
	}
	#endregion
}
//class

public class RoutineSequence
{
	public RoutineSequence listPrevious = null;
	public RoutineSequence listNext = null;

	public Routine currentCoroutine = null;
	public int currentFiber = -1;
	public IEnumerable[] fibers;

	public RoutineSequence(IEnumerable[] fibers)
	{
		this.fibers = fibers;
	}
}

public class Routine
{
	public Routine listPrevious = null;
	public Routine listNext = null;
	public IEnumerable fiberSource;
	public IEnumerator fiberEnum;
	//public IEnumerator fiberToCopy;
	public bool paused = false;
	public bool finished = false;
	public int waitForFrame = -1;
	public float waitForTime = -1.0f;
	public bool waitForBool;
	public Routine waitForCoroutine;

	//public CoroutineNode(IEnumerable fiberSource)
	//{
	//    this.fiberSource = fiberSource;
	//    this.fiber = fiberSource.GetEnumerator();
	//}

	public Routine()
	{

	}

	public Routine (IEnumerable fiberSource)
	{
		this.fiberSource = fiberSource;
		this.fiberEnum = fiberSource.GetEnumerator ();
	}

	public Routine (IEnumerator fiberEnum)
	{
		this.fiberEnum = fiberEnum;
	}

	public Routine (Routine coroutine)
	{
		this.fiberEnum = coroutine.fiberEnum;
	}

	public Routine SetTo(IEnumerable fiberSource)
	{
		this.fiberSource = fiberSource;
		this.fiberEnum = fiberSource.GetEnumerator ();
		return this;
	}

	public Routine SetTo(IEnumerator fiberEnum)
	{
		this.fiberEnum = fiberEnum;
		return this;
	}

	public void Reset()
	{
		Routine listPrevious = null;
		Routine listNext = null;
		IEnumerable fiberSource;
		IEnumerator fiberEnum;
		paused = false;
		finished = false;
		waitForFrame = -1;
		waitForTime = -1.0f;
		Routine waitForCoroutine = null;
	}
}
