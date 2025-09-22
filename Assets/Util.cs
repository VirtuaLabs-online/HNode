using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class Util
{
    /// <summary>
    /// Helper function for block alpha, to support various automatic masking features
    /// check out <see cref="ShowConfiguration.autoMaskOnZero"/>
    /// </summary>
    /// <param name="channelValue"></param>
    /// <returns></returns>
    internal static byte GetBlockAlpha(byte channelValue)
    {
        if (Loader.showconf.autoMaskOnZero && channelValue == 0)
        {
            return 0;
        }

        return 255;
    }

    /// <summary>
    /// Writes the contents of <paramref name="sourceList"/> into <paramref name="destinationList"/> starting at <paramref name="position"/>.
    /// If <paramref name="destinationList"/> is not large enough to hold the contents of <paramref name="sourceList"/> at <paramref name="position"/>, it will be expanded.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="destinationList"></param>
    /// <param name="sourceList"></param>
    /// <param name="position"></param>
    public static void WriteToListAtPosition<T>(this List<T> destinationList, List<T> sourceList, int position)
    {
        destinationList.EnsureCapacity(position + sourceList.Count);

        //we need to write to the dmx data list directly
        for (int i = position; i < position + sourceList.Count; i++)
        {
            destinationList[i] = sourceList[i - position];
        }
    }

    public class CoarseFineChannelSet
    {
        public byte coarse;
        public byte fine;

        public CoarseFineChannelSet(byte coarse, byte fine)
        {
            this.coarse = coarse;
            this.fine = fine;
        }

        public CoarseFineChannelSet(float value)
        {
            var channelSet = GetCoarseFineChannelRepresentation(value);
            this.coarse = channelSet.coarse;
            this.fine = channelSet.fine;
        }

        /// <summary>
        /// Converts a float value between 0 and 1 to a coarse/fine byte array representation.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static CoarseFineChannelSet GetCoarseFineChannelRepresentation(float value)
        {
            //convert to 16 bit value
            ushort fullValue = (ushort)(Mathf.Clamp01(value) * ushort.MaxValue);
            var bytes = System.BitConverter.GetBytes(fullValue);
            return new CoarseFineChannelSet(bytes[1], bytes[0]);
            /* byte coarse = (byte)Mathf.Floor(value * byte.MaxValue);

            //get a remainding ammount
            double usedbycoarse = Mathf.Floor(value * byte.MaxValue) / byte.MaxValue;
            double newvalue = (value - usedbycoarse);

            byte fine = (byte)Mathf.Round((float)(newvalue * 256 * 255));

            return new CoarseFineChannelSet(coarse, fine); */
        }

        public List<byte> ToList()
        {
            return new List<byte> { coarse, fine };
        }
    }

    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }

    public static void SetHeight(this RectTransform rt, float height)
    {
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    public static void SetPadding(this RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.SetLeft(left);
        rt.SetRight(right);
        rt.SetTop(top);
        rt.SetBottom(bottom);
    }

    private const float FONTSIZE = 24f;
    private const float TEXTWIDTH = 500f;
    private const float ELEMENTHEIGHT = 30f;



    //helper funcs for UI

    internal static ColorBlock normalColorBlock = new ColorBlock
    {
        normalColor = new Color(0.16f, 0.16f, 0.16f, 1),
        highlightedColor = new Color(0.2f, 0.2f, 0.2f, 1),
        pressedColor = new Color(0.3f, 0.3f, 0.3f, 1),
        selectedColor = new Color(0.4f, 0.4f, 0.4f, 1),
        disabledColor = new Color(0.16f, 0.16f, 0.16f, 1),
        colorMultiplier = 1,
        fadeDuration = 0.1f
    };

    internal static void SetRectCenterStretch(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    internal static TextMeshProUGUI AddText(RectTransform rect, string text = "")
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(rect, false);
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        SetRectCenterStretch((RectTransform)textObject.transform);
        textComponent.text = text;
        textComponent.color = Color.white; //default color
        textComponent.fontSize = FONTSIZE;
        //set size
        //SetHeight((RectTransform)textObject.transform, ELEMENTHEIGHT);

        return textComponent;
    }

    internal static Button AddButton(RectTransform rect, string title)
    {
        GameObject buttonObject = new GameObject("Button");
        buttonObject.transform.SetParent(rect, false);
        var button = buttonObject.AddComponent<Button>();
        button.colors = normalColorBlock;
        var img = buttonObject.AddComponent<Image>();
        button.targetGraphic = img;

        ((RectTransform)buttonObject.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ELEMENTHEIGHT);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = title;
        textComponent.fontSize = FONTSIZE;

        SetRectCenterStretch((RectTransform)textObject.transform);

        return button;
    }

    internal static Button WithText(this Button button, string text)
    {
        var textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = text;
        }
        return button;
    }

    internal static Button WithCallback(this Button button, UnityEngine.Events.UnityAction callback)
    {
        button.onClick.AddListener(callback);
        return button;
    }

    internal static Toggle WithValue(this Toggle toggle, bool value)
    {
        toggle.isOn = value;
        return toggle;
    }

    internal static Toggle WithCallback(this Toggle toggle, UnityEngine.Events.UnityAction<bool> callback)
    {
        toggle.onValueChanged.AddListener(callback);
        return toggle;
    }

    internal static Toggle AddToggle(RectTransform rect, string title)
    {
        GameObject toggleObject = new GameObject("Toggle");
        toggleObject.transform.SetParent(rect, false);
        var toggle = toggleObject.AddComponent<Toggle>();
        toggle.colors = normalColorBlock;

        ((RectTransform)toggleObject.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ELEMENTHEIGHT);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(toggleObject.transform, false);
        var textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = FONTSIZE;
        textComponent.text = title;

        SetRectCenterStretch((RectTransform)textObject.transform);

        //background
        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(toggleObject.transform, false);
        var backgroundImage = backgroundObject.AddComponent<Image>();

        SetRectCenterStretch((RectTransform)backgroundObject.transform);

        //set the height of the input field
        ((RectTransform)backgroundObject.transform).SetLeft(TEXTWIDTH);

        toggle.targetGraphic = backgroundImage;

        GameObject checkmarkObject = new GameObject("Checkmark");
        checkmarkObject.transform.SetParent(backgroundObject.transform, false);
        var checkmarkImage = checkmarkObject.AddComponent<Image>();

        SetRectCenterStretch((RectTransform)checkmarkObject.transform);
        ((RectTransform)checkmarkObject.transform).SetPadding(5f, 5f, 5f, 5f);

        toggle.graphic = checkmarkImage;

        return toggle;
    }

    internal static TMP_InputField WithContentType(this TMP_InputField field, TMP_InputField.ContentType contentType)
    {
        field.contentType = contentType;
        return field;
    }

    internal static TMP_InputField WithPlaceholder(this TMP_InputField field, string placeholderText)
    {
        if (field.placeholder is TextMeshProUGUI placeholder)
        {
            placeholder.text = placeholderText;
        }
        return field;
    }

    internal static TMP_InputField WithText(this TMP_InputField field, string text)
    {
        field.text = text;
        return field;
    }

    internal static TMP_InputField WithCallback(this TMP_InputField field, UnityEngine.Events.UnityAction<string> callback)
    {
        field.onEndEdit.AddListener(callback);
        return field;
    }

    internal static TMP_InputField AddInputField(RectTransform rect, string title)
    {
        //setup horizontal layout
        GameObject layoutObject = new GameObject("InputFieldLayout");
        layoutObject.transform.SetParent(rect, false);
        layoutObject.AddComponent<RectTransform>();

        //set height
        ((RectTransform)layoutObject.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ELEMENTHEIGHT);

        var textObj = AddText((RectTransform)layoutObject.transform, title);

        GameObject inputFieldObject = new GameObject("TextInputField");
        inputFieldObject.transform.SetParent(layoutObject.transform, false);
        var inputfield = inputFieldObject.AddComponent<TMP_InputField>();
        inputfield.colors = normalColorBlock;
        //add image
        var img = inputFieldObject.AddComponent<Image>();
        inputfield.targetGraphic = img;

        SetRectCenterStretch((RectTransform)inputFieldObject.transform);

        //set the height of the input field
        ((RectTransform)inputFieldObject.transform).SetLeft(TEXTWIDTH);

        //sub object text area
        GameObject textAreaObject = new GameObject("TextArea");
        textAreaObject.transform.SetParent(inputFieldObject.transform, false);
        textAreaObject.AddComponent<RectMask2D>();
        //stretch
        SetRectCenterStretch((RectTransform)textAreaObject.transform);

        //placeholder and text components
        GameObject placeholderTextObject = new GameObject("Placeholder");
        placeholderTextObject.transform.SetParent(textAreaObject.transform, false);
        var placeholderText = placeholderTextObject.AddComponent<TextMeshProUGUI>();
        placeholderText.fontSize = FONTSIZE;
        SetRectCenterStretch((RectTransform)placeholderTextObject.transform);
        placeholderText.text = "Placeholder....";
        //make it gray
        placeholderText.color = Color.gray;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(textAreaObject.transform, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = FONTSIZE;
        SetRectCenterStretch((RectTransform)textObject.transform);

        inputfield.textComponent = text;
        inputfield.placeholder = placeholderText;
        inputfield.textViewport = textAreaObject.GetComponent<RectTransform>();

        //disable, re enable
        inputfield.enabled = false;
        inputfield.enabled = true;

        return inputfield;
    }
}
