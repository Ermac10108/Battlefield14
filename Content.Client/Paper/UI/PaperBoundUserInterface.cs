using Content.Shared.Paper;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Shared.Paper.PaperComponent;
using Content.Client.Hands.Systems; // BF14
using Robust.Shared.Maths; // BF14

namespace Content.Client.Paper.UI;

[UsedImplicitly]
public sealed class PaperBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PaperWindow? _window;

    private HandsSystem? _hands; // BF14

    public PaperBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaperWindow>();
        _window.OnSaved += InputOnTextEntered;
        _window.OnStroke += SendStroke; // BF14
        _window.OnClear += SendClear; // BF14

        if (EntMan.TryGetComponent<PaperComponent>(Owner, out var paper))
        {
            _window.MaxInputLength = paper.ContentSize;
        }
        if (EntMan.TryGetComponent<PaperVisualsComponent>(Owner, out var visuals))
        {
            _window.InitVisuals(Owner, visuals);
        }

        // BF14: drawing on textless map surfaces is gated on a pen/crayon being held in the active hand.
        _hands = EntMan.System<HandsSystem>();
        _hands.OnPlayerSetActiveHand += OnActiveHandChanged;
        _hands.OnPlayerItemAdded += OnHandItemAdded;
        _hands.OnPlayerItemRemoved += OnHandItemRemoved;
        RefreshMapDrawingState();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.Populate((PaperBoundUserInterfaceState) state);
    }

    private void SendStroke(DrawStroke stroke) // BF14
    {
        SendMessage(new PaperDrawStrokeMessage(stroke));
    }

    private void SendClear() // BF14
    {
        SendMessage(new PaperClearMessage());
    }

    private void OnActiveHandChanged(string? hand) // BF14
    {
        RefreshMapDrawingState();
    }

    private void OnHandItemAdded(string hand, EntityUid item) // BF14
    {
        RefreshMapDrawingState();
    }

    private void OnHandItemRemoved(string hand, EntityUid item) // BF14
    {
        RefreshMapDrawingState();
    }

    /// <summary>
    ///     BF14: Re-evaluates whether the local player is holding a writing instrument (pen/crayon)
    ///     in the active hand and updates the map drawing surface accordingly.
    /// </summary>
    private void RefreshMapDrawingState()
    {
        if (_window == null || _hands == null)
            return;

        var active = _hands.GetActiveHandEntity();
        if (active is { } held && EntMan.TryGetComponent<StampComponent>(held, out var stamp))
        {
            _window.SetMapDrawingState(true, stamp.StampedColor);
        }
        else
        {
            _window.SetMapDrawingState(false, Color.Black);
        }
    }

    private void InputOnTextEntered(string text)
    {
        SendMessage(new PaperInputTextMessage(text));

        if (_window != null)
        {
            _window.Input.TextRope = Rope.Leaf.Empty;
            _window.Input.CursorPosition = new TextEdit.CursorPos(0, TextEdit.LineBreakBias.Top);
        }
    }

    protected override void Dispose(bool disposing) // BF14
    {
        base.Dispose(disposing);
        if (_window != null)
        {
            _window.OnSaved -= InputOnTextEntered;
            _window.OnStroke -= SendStroke;
            _window.OnClear -= SendClear;
        }
        if (_hands != null) // BF14
        {
            _hands.OnPlayerSetActiveHand -= OnActiveHandChanged;
            _hands.OnPlayerItemAdded -= OnHandItemAdded;
            _hands.OnPlayerItemRemoved -= OnHandItemRemoved;
        }
    }
}
