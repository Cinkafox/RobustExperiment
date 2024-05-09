namespace Content.Client.StyleSheet.StyleBox;

[Serializable, DataDefinition]
public abstract partial class StyleBoxData
{
        /// <summary>
        /// Left content margin, in virtual pixels.
        /// </summary>
        [DataField] public float? ContentMarginLeftOverride;

        /// <summary>
        /// Top content margin, in virtual pixels.
        /// </summary>
        [DataField] public float? ContentMarginTopOverride;

        /// <summary>
        /// Right content margin, in virtual pixels.
        /// </summary>
        [DataField] public float? ContentMarginRightOverride;

        /// <summary>
        /// Bottom content margin, in virtual pixels.
        /// </summary>
        [DataField] public float? ContentMarginBottomOverride;

        /// <summary>
        /// Left padding, in virtual pixels.
        /// </summary>
        [DataField]  public float PaddingLeft;

        /// <summary>
        /// Bottom padding, in virtual pixels.
        /// </summary>
        [DataField] public float PaddingBottom;

        /// <summary>
        /// Right padding, in virtual pixels.
        /// </summary>
        [DataField] public float PaddingRight;

        /// <summary>
        /// Top padding, in virtual pixels.
        /// </summary>
        [DataField] public float PaddingTop;

        /// <summary>
        /// Padding, in virtual pixels.
        /// </summary>
        [DataField] public Thickness? Padding;

        public void SetBaseParam<T>(ref T styleBox) where T : Robust.Client.Graphics.StyleBox
        {
            if (ContentMarginBottomOverride is not null)
                styleBox.ContentMarginBottomOverride = ContentMarginBottomOverride;
            if (ContentMarginLeftOverride is not null)
                styleBox.ContentMarginLeftOverride = ContentMarginLeftOverride;
            if (ContentMarginTopOverride is not null)
                styleBox.ContentMarginTopOverride = ContentMarginTopOverride;
            if (ContentMarginRightOverride is not null)
                styleBox.ContentMarginRightOverride = ContentMarginRightOverride;

            if (Padding is not null)
            {
                styleBox.Padding = Padding.Value;
                return;
            }

            styleBox.PaddingBottom = PaddingBottom;
            styleBox.PaddingLeft = PaddingLeft;
            styleBox.PaddingRight = PaddingRight;
            styleBox.PaddingTop = PaddingTop;
        }
}

