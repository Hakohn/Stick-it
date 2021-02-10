using System;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static string ToGUIName(this string word)
    {
        string ans = $"{char.ToUpper(word[0])}";
        for (int i = 1; i < word.Length; i++)
        {
            if (word[i] == '_')
            {
                continue;
            }
            if (char.IsUpper(word[i]) && (i + 1 < word.Length && char.IsLower(word[i + 1])))
            {
                ans += " ";
            }
            ans += word[i];
        }
        return ans;
    }

    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static bool IsFoundIn<T>(this T obj, T[] objs) where T : IEquatable<T>
	{
        foreach(T o in objs)
            if (o.Equals(obj))
                return true;

        return false;
	}
    public static bool IsFoundIn<T>(this T obj, List<T> objs)
	{
        return objs.Contains(obj);
	}

    /// <summary>
    /// Min is included, max is excluded.
    /// </summary>
    public static bool IsBetween<T>(this T value, T min, T max) where T : IComparable<T>
    {
        if (min.CompareTo(value) <= 0 && value.CompareTo(max) < 0)
            return true;
        return false;
    }

    public static float Fractional(this float value)
    {
        return Mathf.Abs(value - Mathf.FloorToInt(value));
    }

    public static Vector3 ToHorizontalV3(this Vector2 vect)
    {
        return new Vector3(vect.x, 0, vect.y);
    }

    public static Vector2 ToVector2(this Vector3 vect)
    {
        return new Vector2(vect.x, vect.y);
    }

    public static Vector2 Flipped(this Vector2 vect)
    {
        return new Vector2(vect.y, vect.x);
    }

    public static Vector3 Horizontal(this Vector3 vect)
    {
        return new Vector3(vect.x, 0, vect.z);
    }
    public static Vector3 Vertical(this Vector3 vect)
    {
        return new Vector3(0, vect.y, 0);
    }

    public static Vector3 OppositeDirection(this Vector3 vect)
    {
        return -vect.normalized;
    }

    public static void SetEnabled(this Rigidbody rb, bool enabled)
    {
        if (enabled)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }
        else
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
    }

    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    public static Color ToHDRColor(this Color color, float intensity)
    {
        return color * Mathf.Pow(2, intensity);
    }

    public static float HDRIntensity(this Color color)
    {
        float maxVal = Mathf.Max(color.linear.r, color.linear.g, color.linear.b);
        float intensityPow2 = maxVal / 255f;
        if (intensityPow2 > 1)
            return Mathf.Log(intensityPow2, 2);
        else return 0;
    }

    public static T RandomElement<T>(this T[] arr)
    {
        return arr[UnityEngine.Random.Range(0, arr.Length)];
    }
    public static T RandomElement<T>(this List<T> arr)
    {
        return arr[UnityEngine.Random.Range(0, arr.Count)];
    }

    //   public static float AngleBetweenDirectionAndPoint(Vector3 forward, Vector3 point)
    //{
    //       float angle = Quaternion.LookRotation((point - forward).normalized).eulerAngles.y;
    //       float angleBetweenPoints = Mathf.DeltaAngle(waypoint.transform.rotation.eulerAngles.y, angle);
    //       return angleBetweenPoints;
    //   }
}
