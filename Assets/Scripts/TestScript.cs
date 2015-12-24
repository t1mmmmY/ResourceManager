using UnityEngine;
using System.Collections;

public enum CustomEnum
{
	None,
	Value1,
	Value2,
	Value3
}

public class TestScript : MonoBehaviour
{
	public string TestString = "test string";
	public int TestInt = 10;
	public float TestFloat = 0.99f;
	public CustomEnum TestEnum = CustomEnum.Value1;
}
