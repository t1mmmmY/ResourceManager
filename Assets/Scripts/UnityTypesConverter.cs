using UnityEngine;
using System;

class UnityTypesConverter
{
	// Vector3 section
	public static string Vector3ToString(Vector3 v)
	{
		return string.Format("{0:0.00000},{1:0.00000},{2:0.00000}", v.x, v.y, v.z);
	}

	public static Vector3 Vector3FromString(String s)
	{
		string[] parts = s.Split(new string[] { "," }, StringSplitOptions.None);
		return new Vector3(
			float.Parse(parts[0]),
			float.Parse(parts[1]),
			float.Parse(parts[2]));
	}

	// Quaternion section
	public static string QuaternionToString(Quaternion q)
	{
		return string.Format("{0:0.00000},{1:0.00000},{2:0.00000},{3:0.00000}", q.x, q.y, q.z, q.w);
	}

	public static Quaternion QuaternionFromString(String s)
	{
		string[] parts = s.Split(new string[] { "," }, StringSplitOptions.None);
		return new Quaternion(
			float.Parse(parts[0]),
			float.Parse(parts[1]),
			float.Parse(parts[2]),
			float.Parse(parts[3]));
	}
}