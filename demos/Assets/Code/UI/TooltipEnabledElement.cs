﻿using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

namespace Goldenwere.Unity.UI
{
    /// <summary>
    /// 
    /// </summary>
    public enum AnchorMode
    {
        /// <summary>
        /// Tooltip follows cursor
        /// </summary>
        AttachedToCursor,
        /// <summary>
        /// Tooltip stays fixed and positions based on element
        /// </summary>
        AttachedToElement
    }

    /// <summary>
    /// Defines how the tooltip should be anchored
    /// </summary>
    public enum AnchorPosition
    {
        TopLeft,
        TopMiddle,
        TopRight,
        CenterLeft,
        CenterMiddle,
        CenterRight,
        BottomLeft,
        BottomMiddle,
        BottomRight
    }

    /// <summary>
    /// Defines how the tooltip should be transitioned
    /// </summary>
    public enum TransitionMode
    {
        None,
        Fade
    }

    /// <summary>
    /// Adds a tooltip to a UI element
    /// </summary>
    public class TooltipEnabledElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Fields
#pragma warning disable 0649
        [Tooltip         ("Needed in order to ensure proper tooltip positioning; can be left unassigned as long as the UI element itself is attached to a canvas")]
        [SerializeField] private Camera         cameraThatRendersCanvas;
        [Tooltip         ("Optional string to provide if cannot attach camera in inspector (e.g. prefabbed UI elements instantiated at runtime)")]
        [SerializeField] private string         cameraThatRendersCanvasName;
        [Tooltip         ("Needed in order to ensure proper tooltip positioning as well as attaching tooltip to canvas")]
        [SerializeField] private Canvas         canvasToBeAttachedTo;
        [Tooltip         ("Defines how the tooltip is attached")]
        [SerializeField] private AnchorMode     tooltipAnchorMode;
        [Tooltip         ("The default anchor position. If the tooltip text overflows with this anchor, will change to another one if needed")]
        [SerializeField] private AnchorPosition tooltipAnchorPosition;
        [Range(0.01f,1)] [Tooltip               ("Determines how much the tooltip anchors to the left/right when AnchorPosition is one of the left/right settings (has no effect on Middle settings)")]
        [SerializeField] private float          tooltipHorizontalFactor = 1;
        [Tooltip         ("Prefab which the topmost gameobject can be resized based on text and contains a text element that can be set\n" +
                          "Note: Make sure that the text element has the horizontal+vertical stretch anchor preset and equivalent padding on all sides," +
                          "as this class depends on the left padding when determining container height + bottom padding\n" +
                          "Make sure that the container uses the center+center anchor preset, as this class needs to use its own anchor method due to depending on cursor position")]
        [SerializeField] private GameObject     tooltipPrefab;
        [Tooltip         ("The text to display in the tooltip")]
        [SerializeField] private string         tooltipText;
        [Tooltip         ("How long tooltip transitions last (only used if tooltipTransitionMode isn't set to None")]
        [SerializeField] private float          tooltipTransitionDuration;
        [Tooltip         ("The curve for animating transitions when transitioning into existence")]
        [SerializeField] private AnimationCurve tooltipTransitionCurveIn;
        [Tooltip         ("The curve for animating transitions when transitioning out of existence")]
        [SerializeField] private AnimationCurve tooltipTransitionCurveOut;
        [Tooltip         ("How the tooltip is transitioned/animated into/out of existence")]
        [SerializeField] private TransitionMode tooltipTransitionMode;
        [Tooltip         ("Values used if defining a string that needs formatting. Leave blank if no formatting is done inside tooltipText")]
        [SerializeField] private double[]       tooltipValues;
#pragma warning restore 0649
        /**************/ private bool           isActive;
        /**************/ private bool           isInitialized;
        /**************/ private TooltipPrefab  tooltipSpawnedElement;
        #endregion
        #region Methods
        /// <summary>
        /// Sets up the tooltip at start
        /// </summary>
        private void Start()
        {
            if (!isInitialized)
                Initialize();
            SetText();

            if (tooltipAnchorMode == AnchorMode.AttachedToElement)
            {
                RectTransform thisRect = GetComponent<RectTransform>();
                Vector2 newPos = Vector2.zero;

                switch (tooltipAnchorPosition)
                {
                    case AnchorPosition.TopLeft:
                        newPos.x -= ((tooltipSpawnedElement.RTransform.sizeDelta.x / 2) + (thisRect.sizeDelta.x / 2)) * tooltipHorizontalFactor;
                        newPos.y += (tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (thisRect.sizeDelta.y / 2);

                        if (tooltipSpawnedElement.ArrowEnabled)
                        {
                            newPos.y += (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y);
                            tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0,
                                -((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                            tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                        }
                        break;
                    case AnchorPosition.TopMiddle:
                        newPos.y += (tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (thisRect.sizeDelta.y / 2);

                        if (tooltipSpawnedElement.ArrowEnabled)
                        {
                            newPos.y += (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y);
                            tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0,
                                -((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                            tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                        }
                        break;
                    case AnchorPosition.TopRight:
                        newPos.x += ((tooltipSpawnedElement.RTransform.sizeDelta.x / 2) + (thisRect.sizeDelta.x / 2)) * tooltipHorizontalFactor;
                        newPos.y += (tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (thisRect.sizeDelta.y / 2);

                        if (tooltipSpawnedElement.ArrowEnabled)
                        {
                            newPos.y += (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y);
                            tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0,
                                -((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                            tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                        }
                        break;
                    case AnchorPosition.CenterLeft:
                        newPos.x -= ((tooltipSpawnedElement.RTransform.sizeDelta.x / 2) + (thisRect.sizeDelta.x / 2)) * tooltipHorizontalFactor;

                        if (tooltipSpawnedElement.ArrowEnabled)
                        {
                            newPos.y += (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y);
                            tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0,
                                -((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                            tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                        }
                        break;
                    case AnchorPosition.CenterRight:
                        newPos.x += ((tooltipSpawnedElement.RTransform.sizeDelta.x / 2) + (thisRect.sizeDelta.x / 2)) * tooltipHorizontalFactor;

                        if (tooltipSpawnedElement.ArrowEnabled)
                        {
                            newPos.y += (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y);
                            tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0,
                                -((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                            tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                        }
                        break;
                    case AnchorPosition.BottomLeft:
                        newPos.x -= ((tooltipSpawnedElement.RTransform.sizeDelta.x / 2) + (thisRect.sizeDelta.x / 2)) * tooltipHorizontalFactor;
                        newPos.y -= (tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (thisRect.sizeDelta.y / 2);

                        if (tooltipSpawnedElement.ArrowEnabled)
                        {
                            newPos.y -= (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y);
                            tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0,
                                ((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                            tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 180);
                        }
                        break;
                    case AnchorPosition.BottomMiddle:
                        newPos.y -= (tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (thisRect.sizeDelta.y / 2);

                        if (tooltipSpawnedElement.ArrowEnabled)
                        {
                            newPos.y -= (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y);
                            tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0,
                                ((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                            tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 180);
                        }
                        break;
                    case AnchorPosition.BottomRight:
                        newPos.x += ((tooltipSpawnedElement.RTransform.sizeDelta.x / 2) + (thisRect.sizeDelta.x / 2)) * tooltipHorizontalFactor;
                        newPos.y -= (tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (thisRect.sizeDelta.y / 2);

                        if (tooltipSpawnedElement.ArrowEnabled)
                        {
                            newPos.y -= (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y);
                            tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0,
                                ((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                            tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 180);
                        }
                        break;

                    case AnchorPosition.CenterMiddle:
                    default:
                        if (tooltipSpawnedElement.ArrowEnabled)
                        {
                            newPos.y += (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y);
                            tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0,
                                -((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                            tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                        }
                        break;
                }

                tooltipSpawnedElement.RTransform.anchoredPosition = newPos;
            }
        }

        /// <summary>
        /// Set position of tooltip at Update
        /// </summary>
        private void Update()
        {
            if (isActive)
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                    SetActive(false);

                else if (tooltipAnchorMode == AnchorMode.AttachedToCursor &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasToBeAttachedTo.transform as RectTransform, Mouse.current.position.ReadValue(), cameraThatRendersCanvas, out Vector2 newPos))
                {
                    switch (tooltipAnchorPosition)
                    {
                        case AnchorPosition.TopLeft:
                            newPos.x += tooltipSpawnedElement.RTransform.sizeDelta.x / 2 * tooltipHorizontalFactor;
                            newPos.y -= tooltipSpawnedElement.RTransform.sizeDelta.y / 2;
                            break;
                        case AnchorPosition.TopMiddle:
                            newPos.y -= tooltipSpawnedElement.RTransform.sizeDelta.y / 2;
                            break;
                        case AnchorPosition.TopRight:
                            newPos.x -= tooltipSpawnedElement.RTransform.sizeDelta.x / 2 * tooltipHorizontalFactor;
                            newPos.y -= tooltipSpawnedElement.RTransform.sizeDelta.y / 2;
                            break;
                        case AnchorPosition.CenterLeft:
                            newPos.x += tooltipSpawnedElement.RTransform.sizeDelta.x / 2 * tooltipHorizontalFactor;
                            break;
                        case AnchorPosition.CenterRight:
                            newPos.x -= tooltipSpawnedElement.RTransform.sizeDelta.x / 2 * tooltipHorizontalFactor;
                            break;
                        case AnchorPosition.BottomLeft:
                            newPos.x += tooltipSpawnedElement.RTransform.sizeDelta.x / 2 * tooltipHorizontalFactor;
                            newPos.y += tooltipSpawnedElement.RTransform.sizeDelta.y / 2;
                            break;
                        case AnchorPosition.BottomMiddle:
                            newPos.y += tooltipSpawnedElement.RTransform.sizeDelta.y / 2;
                            break;
                        case AnchorPosition.BottomRight:
                            newPos.x -= tooltipSpawnedElement.RTransform.sizeDelta.x / 2 * tooltipHorizontalFactor;
                            newPos.y += tooltipSpawnedElement.RTransform.sizeDelta.y / 2;
                            break;

                        case AnchorPosition.CenterMiddle:
                        default:
                            // Do nothing in this case - newPos should already be centered if the notes for tooltipPrefab are followed
                            break;
                    }

                    #region Position clamp-to-screen
                    Rect canvasRect = (canvasToBeAttachedTo.transform as RectTransform).rect;
                    if (newPos.x < canvasRect.xMin + tooltipSpawnedElement.RTransform.sizeDelta.x / 2)
                        newPos.x = canvasRect.xMin + tooltipSpawnedElement.RTransform.sizeDelta.x / 2;
                    if (newPos.x + tooltipSpawnedElement.RTransform.sizeDelta.x / 2 > canvasRect.xMax)
                        newPos.x = canvasRect.xMax - tooltipSpawnedElement.RTransform.sizeDelta.x / 2;
                    if (newPos.y < canvasRect.yMin + tooltipSpawnedElement.RTransform.sizeDelta.y / 2)
                        newPos.y = canvasRect.yMin + tooltipSpawnedElement.RTransform.sizeDelta.y / 2;
                    if (newPos.y + tooltipSpawnedElement.RTransform.sizeDelta.y / 2 > canvasRect.yMax)
                        newPos.y = canvasRect.yMax - tooltipSpawnedElement.RTransform.sizeDelta.y / 2;
                    #endregion

                    tooltipSpawnedElement.RTransform.anchoredPosition = newPos;

                    if (tooltipSpawnedElement.ArrowEnabled)
                    {
                        switch (tooltipAnchorPosition)
                        {
                            case AnchorPosition.TopLeft:
                            case AnchorPosition.TopMiddle:
                            case AnchorPosition.TopRight:
                                tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0, 
                                    (tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2));
                                tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 180);
                                break;
                            case AnchorPosition.BottomLeft:
                            case AnchorPosition.BottomMiddle:
                            case AnchorPosition.BottomRight:
                                tooltipSpawnedElement.Arrow.rectTransform.anchoredPosition = new Vector2(0, 
                                    -((tooltipSpawnedElement.RTransform.sizeDelta.y / 2) + (tooltipSpawnedElement.Arrow.rectTransform.sizeDelta.y / 2)));
                                tooltipSpawnedElement.Arrow.rectTransform.rotation = Quaternion.Euler(0, 0, 0);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Destroys the tooltip when the enabled element itself is destroyed
        /// </summary>
        private void OnDestroy()
        {
            Destroy(tooltipSpawnedElement);
        }

        /// <summary>
        /// Initializes the tooltip; this is separate from Start in case SetText is called externally before Start gets a chance to run
        /// </summary>
        private void Initialize()
        {
            if (cameraThatRendersCanvas == null)
                if (cameraThatRendersCanvasName != null && cameraThatRendersCanvasName != "")
                    cameraThatRendersCanvas = GameObject.Find(cameraThatRendersCanvasName).GetComponent<Camera>();
                else
                    cameraThatRendersCanvas = Camera.main;
            if (canvasToBeAttachedTo == null)
                canvasToBeAttachedTo = gameObject.GetComponentInParents<Canvas>();

            if (tooltipAnchorMode == AnchorMode.AttachedToCursor)
                tooltipSpawnedElement = Instantiate(tooltipPrefab, canvasToBeAttachedTo.transform).GetComponent<TooltipPrefab>();
            else
                tooltipSpawnedElement = Instantiate(tooltipPrefab, GetComponent<RectTransform>()).GetComponent<TooltipPrefab>();
            isActive = tooltipSpawnedElement.gameObject.activeSelf;
            SetActive(false, TransitionMode.None);
            isInitialized = true;
        }

        /// <summary>
        /// OnPointerEnter, enable the tooltip
        /// </summary>
        public void OnPointerEnter(PointerEventData data)
        {
            StopAllCoroutines();
            SetActive(true);
        }

        /// <summary>
        /// OnPointerExit, disable the tooltip
        /// </summary>
        public void OnPointerExit(PointerEventData data)
        {
            StopAllCoroutines();
            SetActive(false);
        }

        /// <summary>
        /// Set the tooltip's colors with this method
        /// </summary>
        /// <param name="background">Color applied to any non-text graphic</param>
        /// <param name="foreground">Color applied to any text graphic</param>
        public void SetColors(Color background, Color foreground)
        {
            if (!isInitialized)
                Initialize();

            Graphic[] graphics = tooltipSpawnedElement.GetComponentsInChildren<Graphic>();
            foreach(Graphic graphic in graphics)
            {
                if (graphic.GetType().Namespace == "TMPro")
                    graphic.color = foreground;
                else
                    graphic.color = background;
            }
        }

        /// <summary>
        /// Updates the tooltip text with a new tooltip and optional new values (required if the tooltip has formated values)
        /// </summary>
        /// <param name="newTooltip">The new tooltip string value</param>
        /// <param name="newValues">New values to display in the tooltip if the string requires formatting</param>
        public void UpdateText(string newTooltip, double[] newValues = null)
        {
            tooltipText = newTooltip;
            tooltipValues = newValues;
            SetText();
        }

        /// <summary>
        /// Coroutine for the Fade transition
        /// </summary>
        /// <param name="_isActive">Determines whether to fade in or out</param>
        private IEnumerator TransitionFade(bool _isActive)
        {
            float t = 0;
            while (t <= tooltipTransitionDuration)
            {
                if (_isActive)
                    tooltipSpawnedElement.CGroup.alpha = tooltipTransitionCurveIn.Evaluate(t / tooltipTransitionDuration);
                else
                    tooltipSpawnedElement.CGroup.alpha = tooltipTransitionCurveOut.Evaluate(t / tooltipTransitionDuration);

                yield return null;
                t += Time.deltaTime;
            }

            if (_isActive)
                tooltipSpawnedElement.CGroup.alpha = 1;
            else
                tooltipSpawnedElement.CGroup.alpha = 0;
        }

        /// <summary>
        /// Activates/deactivates the tooltip, which engages in transitions if the tooltip's active state is different from the new state
        /// </summary>
        /// <param name="_isActive">Whether to activate or deactivate the tooltip</param>
        private void SetActive(bool _isActive)
        {
            SetActive(_isActive, tooltipTransitionMode);
        }

        /// <summary>
        /// Activates/deactivates the tooltip, which engages in transitions if the tooltip's active state is different from the new state
        /// <para>This overload overrides the mode defined for the element</para>
        /// </summary>
        /// <param name="_isActive">Whether to activate or deactivate the tooltip</param>
        /// <param name="mode">The mode of transition to use for animation</param>
        private void SetActive(bool _isActive, TransitionMode mode)
        {
            if (isActive != _isActive || !isActive)
            {
                isActive = _isActive;
                switch (mode)
                {
                    case TransitionMode.Fade:
                        if (!tooltipSpawnedElement.gameObject.activeSelf)
                            tooltipSpawnedElement.gameObject.SetActive(true);
                        if (!isActive && tooltipSpawnedElement.CGroup.alpha > 0 || isActive)
                            StartCoroutine(TransitionFade(isActive));
                        break;
                    case TransitionMode.None:
                    default:
                        tooltipSpawnedElement.gameObject.SetActive(isActive);
                        break;
                }
            }
        }

        /// <summary>
        /// Sets the text element text to the stored tooltipText and resizes the container
        /// </summary>
        private void SetText()
        {
            if (!isInitialized)
                Initialize();

            if (tooltipValues != null && tooltipValues.Length > 0)
                tooltipSpawnedElement.Text.text = string.Format(tooltipText, tooltipValues).RepairSerializedEscaping();
            else
                tooltipSpawnedElement.Text.text = tooltipText.RepairSerializedEscaping();

            tooltipSpawnedElement.RTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                tooltipSpawnedElement.Text.preferredHeight + tooltipSpawnedElement.Text.rectTransform.offsetMin.y * 2);
        }
        #endregion
    }
}
