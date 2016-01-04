using UnityEngine;
using System;
using System.ComponentModel;
using System.Globalization;

/// <summary>
/// Abstract UI rectangle containing functionality common to both panels and widgets.
/// A UI rectangle contains 4 anchor points (one for each side), and it ensures that they are updated in the proper order.
/// </summary>

class CustomAnchorPointConverter : TypeConverter
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
//			UIRect.AnchorPoint anchor = new UIRect.AnchorPoint();

			//Find UIRect in scene
			UIRect[] allRectsInScene = GameObject.FindObjectsOfType<UIRect>();
			int rectHash = int.Parse(parts[0]);
			UIRect uiRect = null;
			foreach (UIRect rect in allRectsInScene)
			{
				if (rect.GetHashCode() == rectHash)
				{
					uiRect = rect;
				}
			}
//			UIRect.AnchorPoint anchor = uiRect.leftAnchor;

			//Find target transform in scene
			Transform[] allTransformsInScene = GameObject.FindObjectsOfType<Transform>();
			Transform target = null;
			int transformHash = int.Parse(parts[2]);
			foreach (Transform trans in allTransformsInScene)
			{
				if (trans.GetHashCode() == transformHash)
				{
					target = trans;
				}
			}

			int anchorHash = int.Parse(parts[1]);

			int leftAnchorHash = uiRect.leftAnchor.GetHashCode();
			int rightAnchorHash = uiRect.rightAnchor.GetHashCode();
			int topAnchorHash = uiRect.topAnchor.GetHashCode();
			int bottomAnchorHash = uiRect.bottomAnchor.GetHashCode();

			if (anchorHash == leftAnchorHash)
			{
				uiRect.leftAnchor.Set(target, float.Parse(parts[3]), float.Parse(parts[4]));
				return uiRect.leftAnchor;
			}
			else if (anchorHash == rightAnchorHash)
			{
				uiRect.rightAnchor.Set(target, float.Parse(parts[3]), float.Parse(parts[4]));
				return uiRect.rightAnchor;
			}
			else if (anchorHash == topAnchorHash)
			{
				uiRect.topAnchor.Set(target, float.Parse(parts[3]), float.Parse(parts[4]));
				return uiRect.topAnchor;
			}
			else if (anchorHash == bottomAnchorHash)
			{
				uiRect.bottomAnchor.Set(target, float.Parse(parts[3]), float.Parse(parts[4]));
				return uiRect.bottomAnchor;
			}
			else
			{
				Debug.LogError("Can't find anchor hash!");
				return null;	
			}

		}
		return base.ConvertFrom(context, culture, value);
	}

	// Overrides the ConvertTo method of TypeConverter.
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{  
		if (destinationType == typeof(string))
		{
			var obj = (UIRect.AnchorPoint)value;
			return string.Format("{0},{1},{2},{3:0.00000},{4}", 
				obj.rect.GetHashCode(), obj.GetHashCode(), obj.target.GetHashCode(), obj.relative, obj.absolute);
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}