using System.Numerics; // BF14
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

/// <summary>
///     A single freehand stroke drawn on a piece of paper. BF14
///     Points are in untransformed local pixel/screen space; the client maps them by canvas size.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class DrawStroke
{
    [DataField]
    public Color Color = Color.Black;

    [DataField]
    public List<Vector2> Points = new();
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaperComponent : Component
{
    public PaperAction Mode;
    [DataField("content"), AutoNetworkedField]
    public string Content { get; set; } = "";

    [DataField("contentSize")]
    public int ContentSize { get; set; } = 6000;

    [DataField("stampedBy"), AutoNetworkedField]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    /// <summary>
    ///     Freehand drawings on the paper, drawn on top of the paper texture and text. BF14
    /// </summary>
    [DataField("strokes"), AutoNetworkedField]
    public List<DrawStroke> Strokes { get; set; } = new();

    /// <summary>
    ///     The color of the instrument currently being used to draw. Set server-side when the paper is opened with a pen/crayon; used by clients as the default draw color. BF14
    /// </summary>
    [DataField("currentColor"), AutoNetworkedField]
    public Color CurrentColor = Color.Black;

    /// <summary>
    ///     Stamp to be displayed on the paper, state from bureaucracy.rsi
    /// </summary>
    [DataField("stampState"), AutoNetworkedField]
    public string? StampState { get; set; }

    [DataField, AutoNetworkedField]
    public bool EditingDisabled;

    /// <summary>
    /// Sound played after writing to the paper.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound { get; private set; } = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));

    // Frontier: 
    /// <summary>
    /// Sound played after writing to the paper.
    /// </summary>
    [DataField]
    public bool DestroyOnFax { get; private set; }

    [DataField]
    public string? DestroyMessage { get; private set; }
    // End Frontier

    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Text;
        public readonly List<StampDisplayInfo> StampedBy;
        public readonly PaperAction Mode;
        public readonly List<DrawStroke> Strokes;
        public readonly Color Color;

        public PaperBoundUserInterfaceState(string text, List<StampDisplayInfo> stampedBy, PaperAction mode = PaperAction.Read,
            List<DrawStroke>? strokes = null, Color color = default)
        {
            Text = text;
            StampedBy = stampedBy;
            Mode = mode;
            Strokes = strokes ?? new();
            Color = color;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly string Text;

        public PaperInputTextMessage(string text)
        {
            Text = text;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperDrawStrokeMessage : BoundUserInterfaceMessage
    {
        public readonly DrawStroke Stroke;

        public PaperDrawStrokeMessage(DrawStroke stroke)
        {
            Stroke = stroke;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperClearMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public enum PaperUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum PaperAction
    {
        Read,
        Write,
    }

    [Serializable, NetSerializable]
    public enum PaperVisuals : byte
    {
        Status,
        Stamp
    }

    [Serializable, NetSerializable]
    public enum PaperStatus : byte
    {
        Blank,
        Written
    }
}
