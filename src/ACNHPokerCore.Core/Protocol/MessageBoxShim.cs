using System;

namespace ACNHPokerCore.Core
{
    // Minimal stand-ins for the System.Windows.Forms enums/types that Utilities.cs and
    // USBBot.cs pass around when reporting protocol errors. Only the members actually
    // used by the ported code exist here - this is not a general WinForms replacement.
    public enum MessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel,
    }

    public enum MessageBoxIcon
    {
        None,
        Information,
        Warning,
        Error,
        Question,
    }

    public enum DialogResult
    {
        None,
        OK,
        Cancel,
        Yes,
        No,
    }

    /// <summary>
    /// The original WinForms code reports protocol-level errors with
    /// <c>System.Windows.Forms.MessageBox.Show(...)</c> calls scattered through
    /// <see cref="Utilities"/> (about a hundred call sites). This core library must not
    /// reference System.Windows.Forms, so this is a drop-in replacement with the same
    /// call surface (only the two simple overloads that Utilities.cs actually uses).
    ///
    /// Instead of popping a dialog itself, it raises <see cref="ErrorReported"/> so the
    /// Avalonia UI (or a future CLI/test host) decides how to present it. If nothing is
    /// subscribed, the message is written to <see cref="Console.Error"/> so it isn't lost.
    /// </summary>
    public static class MessageBox
    {
        public static event Action<string, string>? ErrorReported;

        public static void Show(string text) => Show(text, string.Empty);

        public static void Show(string text, string caption)
        {
            if (ErrorReported is null)
            {
                Console.Error.WriteLine(string.IsNullOrEmpty(caption) ? text : $"[{caption}] {text}");
                return;
            }

            ErrorReported.Invoke(text, caption);
        }
    }

    /// <summary>
    /// Stand-in for the original app's Custom/MyMessageBox.cs (a themed WinForms dialog),
    /// used by USBBot.cs and one call site in Utilities.cs. Same rationale as
    /// <see cref="MessageBox"/> above - forwards to the UI via an event instead of drawing
    /// anything itself. The full themed dialog (Custom/MyMessageBox.cs, ~795 lines of GDI+)
    /// was not ported; a future session can build an Avalonia equivalent and subscribe here.
    /// </summary>
    public static class MyMessageBox
    {
        public static event Action<string, string, MessageBoxButtons, MessageBoxIcon>? MessageRequested;

        public static DialogResult Show(string text) => Show(text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None);

        public static DialogResult Show(string text, string caption) => Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons) => Show(text, caption, buttons, MessageBoxIcon.None);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (MessageRequested is null)
            {
                Console.Error.WriteLine(string.IsNullOrEmpty(caption) ? text : $"[{caption}] {text}");
            }
            else
            {
                MessageRequested.Invoke(text, caption, buttons, icon);
            }

            // Headless/no-UI fallback: nothing pressed "Yes", so callers that branch on the
            // result (e.g. "show troubleshooting details?") take the safe/quiet path.
            return DialogResult.None;
        }
    }
}
