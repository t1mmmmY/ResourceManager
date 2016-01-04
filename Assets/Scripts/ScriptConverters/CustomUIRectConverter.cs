using UnityEngine;
using System;
using System.ComponentModel;
using System.Globalization;

/// <summary>
/// Abstract UI rectangle containing functionality common to both panels and widgets.
/// A UI rectangle contains 4 anchor points (one for each side), and it ensures that they are updated in the proper order.
/// </summary>

class CustomUIRectConverter : TypeConverter
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
			string[] parts = ((string)value).Split(new char[] {';'});

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

			UIRect.AnchorPoint tempAnchor = JsonUtility.FromJson<UIRect.AnchorPoint>(parts[1]);
			uiRect.leftAnchor.Set(tempAnchor.target, tempAnchor.relative, tempAnchor.absolute);

			tempAnchor = JsonUtility.FromJson<UIRect.AnchorPoint>(parts[2]);
			uiRect.rightAnchor.Set(tempAnchor.target, tempAnchor.relative, tempAnchor.absolute);

			tempAnchor = JsonUtility.FromJson<UIRect.AnchorPoint>(parts[3]);
			uiRect.topAnchor.Set(tempAnchor.target, tempAnchor.relative, tempAnchor.absolute);

			tempAnchor = JsonUtility.FromJson<UIRect.AnchorPoint>(parts[4]);
			uiRect.bottomAnchor.Set(tempAnchor.target, tempAnchor.relative, tempAnchor.absolute);


			uiRect.UpdateAnchors();
			uiRect.Update();

			return uiRect;

		}
		return base.ConvertFrom(context, culture, value);
	}

	// Overrides the ConvertTo method of TypeConverter.
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{  
		if (destinationType == typeof(string))
		{
			var obj = (UIRect)value;
			return string.Format("{0};{1};{2};{3};{4}", 
				obj.GetHashCode(), JsonUtility.ToJson(obj.leftAnchor), JsonUtility.ToJson(obj.rightAnchor), 
				JsonUtility.ToJson(obj.topAnchor), JsonUtility.ToJson(obj.bottomAnchor));
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}