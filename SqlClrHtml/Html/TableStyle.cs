using System;
using System.Collections.Generic;

namespace SimpleTalk.SqlClr.Html
{
    /// <summary>
    /// The look of a table. The built-in styles are written into each element's style attribute,
    /// because some e-mail clients drop a style sheet in the head; a custom style is the caller's
    /// own style sheet, applied by class.
    /// </summary>
    internal sealed class TableStyle
    {
        public const string Default = "ST_BLUE";

        private const string Font = "font-family: Arial, Helvetica, sans-serif; font-size: 12px;";
        private const string Cell = "padding: 2px 4px; text-align: left; vertical-align: top;";

        private TableStyle(string name, string? styleSheet, string? className, Palette? palette)
        {
            Name = name;
            StyleSheet = styleSheet;
            ClassName = className;
            _palette = palette;
        }

        private readonly Palette? _palette;

        /// <summary>The name, ST_BLUE and the others, or CUSTOM.</summary>
        public string Name { get; }

        /// <summary>A custom style's style sheet, for the head.</summary>
        public string? StyleSheet { get; }

        /// <summary>A custom style's class for the table: the name in its first table.name selector.</summary>
        public string? ClassName { get; }

        public string? Table => _palette == null ? null : $"border-collapse: collapse; width: 100%; {Font}";

        public string? Caption => _palette == null ? null
            : $"background-color: {_palette.CaptionBackground}; color: {_palette.CaptionText}; font-weight: bold; {Cell}";

        public string? Header => _palette == null ? null
            : $"background-color: {_palette.Header}; color: {_palette.HeaderText}; font-weight: bold; border: 1px solid {_palette.HeaderBorder}; {Cell}";

        public string? Data(int row) => _palette == null ? null
            : $"color: {_palette.Text}; border: 1px solid {_palette.Border}; {Cell}" + (row % 2 == 1 ? $" background-color: {_palette.Alternate};" : "");

        public string? Footer => _palette == null ? null
            : $"color: {_palette.Text}; border: 1px solid {_palette.Border}; font-size: 11px; {Cell}";

        /// <summary>
        /// A built-in style by name, ST_BLUE when there is none, or the caller's style sheet when
        /// it contains a style element.
        /// </summary>
        public static TableStyle Parse(string? style)
        {
            if (style == null || style.Trim().Length == 0)
                style = Default;

            if (style.IndexOf("<style", StringComparison.OrdinalIgnoreCase) >= 0)
                return new TableStyle("CUSTOM", style, CustomClassName(style), null);

            var name = style.Trim().ToUpperInvariant();
            var palettes = Palettes();
            if (name == "ST_NOSTYLE")
                return new TableStyle(name, null, null, null);
            if (!palettes.TryGetValue(name, out var palette))
                throw new HtmlException(
                    $"@style '{style}' is not a style: use ST_BLUE, ST_RED, ST_GREEN, ST_ROSE, ST_BROWN, ST_BLACK, ST_SIMPLE or ST_NOSTYLE, or a style element of your own.");
            return new TableStyle(name, null, null, palette);
        }

        // In version 1, the table of a custom style took its class from its first "table.name" selector
        private static string? CustomClassName(string style)
        {
            var at = style.IndexOf("table.", StringComparison.OrdinalIgnoreCase);
            if (at < 0)
                return null;
            var start = at + "table.".Length;
            var end = start;
            while (end < style.Length && (char.IsLetterOrDigit(style[end]) || style[end] == '_' || style[end] == '-'))
                end++;
            return end > start ? style.Substring(start, end - start) : null;
        }

        // The colours of version 1's styles
        private static Dictionary<string, Palette> Palettes() => new Dictionary<string, Palette>
        {
            ["ST_BLUE"] = new Palette("#006699", "#FFFFFF", "#0070A8", "#00557F", "#E1EEF4", "#E1EEF4", "#E1EEF4", "#00557F"),
            ["ST_RED"] = new Palette("#991821", "#FFFFFF", "#B01C26", "#80141C", "#F7CDCD", "#F7CDCD", "#F7CDCD", "#80141C"),
            ["ST_GREEN"] = new Palette("#36752D", "#FFFFFF", "#36752D", "#275420", "#C6FFC2", "#DFFFDE", "#DFFFDE", "#275420"),
            ["ST_ROSE"] = new Palette("#652299", "#FFFFFF", "#714399", "#4D1A75", "#E7BDFF", "#F4E3FF", "#F4E3FF", "#4D1A75"),
            ["ST_BROWN"] = new Palette("#A65B1A", "#FFFFFF", "#BF691E", "#7F4614", "#D9CFB8", "#F0E5CC", "#F0E5CC", "#7F4614"),
            ["ST_BLACK"] = new Palette("#8C8C8C", "#FFFFFF", "#A3A3A3", "#7D7D7D", "#DBDBDB", "#EBEBEB", "#EBEBEB", "#7D7D7D"),
            ["ST_SIMPLE"] = new Palette("#DEDEDE", "#333333", "#666666", "#333333", "#666666", "#FFFFFF", "#FFFFFF", "#333333"),
        };

        private sealed class Palette
        {
            public Palette(string header, string headerText, string headerBorder, string text, string border,
                string alternate, string captionBackground, string captionText)
            {
                Header = header;
                HeaderText = headerText;
                HeaderBorder = headerBorder;
                Text = text;
                Border = border;
                Alternate = alternate;
                CaptionBackground = captionBackground;
                CaptionText = captionText;
            }

            public string Header { get; }
            public string HeaderText { get; }
            public string HeaderBorder { get; }
            public string Text { get; }
            public string Border { get; }
            public string Alternate { get; }
            public string CaptionBackground { get; }
            public string CaptionText { get; }
        }
    }
}
