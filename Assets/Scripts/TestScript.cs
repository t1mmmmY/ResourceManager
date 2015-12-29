using UnityEngine;
using System.Collections;
using System;
using System.ComponentModel;
using System.Globalization;

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
	public CustomClass CustomObject;
}

class CustomClassConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		if (sourceType == typeof(string))
		{
			return true;
		}
		return base.CanConvertFrom(context, sourceType);
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value is string)
		{
			string[] parts = ((string)value).Split(new char[] {','});
			return new CustomClass(parts[0], int.Parse(parts[1]), float.Parse(parts[2]));
		}
		return base.ConvertFrom(context, culture, value);
	}

	// Overrides the ConvertTo method of TypeConverter.
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{  
		if (destinationType == typeof(string))
		{
			var obj = (CustomClass)value;
			return string.Format("{0},{1},{2:0.00000}", obj.A, obj.B, obj.C);
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}

[TypeConverter(typeof(CustomClassConverter))]
[Serializable]
public class CustomClass
{
	public string A;
	public int B;
	public float C;

	public CustomClass(string a, int b, float c)
	{
		A = a;
		B = b;
		C = c;
	}
}