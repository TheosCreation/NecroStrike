using System;
using System.Collections;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private Coroutine intervalCoroutine;

    public void StopTimer()
    {
        StopAllCoroutines();
    }

    // Set a timer with a basic callback
    public void SetTimer(float delay, Action callback)
    {
        StartCoroutine(TimerCoroutine(delay, callback));
    }

    // Set a timer with a parameterized callback
    public void SetTimer<T>(float delay, Action<T> callback, T parameter)
    {
        StartCoroutine(TimerCoroutine(delay, callback, parameter));
    }

    // Set a timer to change a bool after a delay
    public void SetTimer(float delay, Action<bool> callback, bool parameter)
    {
        StartCoroutine(TimerCoroutine(delay, callback, parameter));
    }

    // Set a repeated interval to call a callback
    public void SetInterval<T>(float interval, Action<T> callback, T parameter)
    {
        // Stop any existing interval coroutine
        StopInterval();

        // Start a new interval coroutine
        intervalCoroutine = StartCoroutine(IntervalCoroutine(interval, callback, parameter));
    }

    // Stop the currently running interval
    public void StopInterval()
    {
        if (intervalCoroutine != null)
        {
            StopCoroutine(intervalCoroutine);
            intervalCoroutine = null;
        }
    }

    private IEnumerator TimerCoroutine(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    private IEnumerator TimerCoroutine<T>(float delay, Action<T> callback, T parameter)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke(parameter);
    }

    private IEnumerator TimerCoroutine(float delay, Action<bool> callback, bool parameter)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke(parameter);
    }

    // Coroutine for repeating a callback at set intervals
    private IEnumerator IntervalCoroutine<T>(float interval, Action<T> callback, T parameter)
    {
        while (true) // Continue until stopped
        {
            callback?.Invoke(parameter); // Call the callback
            yield return new WaitForSeconds(interval); // Wait for the specified interval
        }
    }
}