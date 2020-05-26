using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MedianStats
{
	public static class RichTextBoxHelper
	{
		/// <summary>
		/// WARNING: Be carefull with this function.
		/// When the text is wrapped (text longer then the visual-text-box) this function returns only the text until the end of the visual line
		/// As long as there is no text-wrapping it should work fine. (Is disabled now by setting PageWidht to 100000)
		/// If this is a problem this function needs to be extended to handle this case.
		/// </summary>
		public static TextRange GetVisualLine(this RichTextBox richTextBox, int lineNumber)
		{
			TextPointer textStart = richTextBox.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);

			var searchedLineStart = textStart.GetLineStartPosition(lineNumber);
			if (searchedLineStart == null) {
				// Line does not exist
				throw new Exception($"GetLineNotifierLine() - Line {lineNumber} does not exist");
			}
			var nextLineStart = searchedLineStart.GetLineStartPosition(1);
			var searchedLineEnd = (nextLineStart != null ? nextLineStart : textStart.DocumentEnd).GetInsertionPosition(LogicalDirection.Backward);
			TextRange textRange = new TextRange(searchedLineStart, searchedLineEnd);

			return textRange;
		}

		public static void SetLineBackgroundColor(this RichTextBox richTextBox, int lineNumber, Brush brush)
		{
			var line = GetVisualLine(richTextBox, lineNumber);
			line.ApplyPropertyValue(TextElement.BackgroundProperty, brush);
		}

		public static void SetPropertyForAllText(this RichTextBox richTextBox, DependencyProperty formattingProperty, object value)
		{
			TextRange textRangeAll = GetAllText(richTextBox);

			textRangeAll.ApplyPropertyValue(formattingProperty, value);
		}

		public static TextRange GetAllText(this RichTextBox richTextBox)
		{
			TextPointer textStart = richTextBox.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);
			TextRange textRangeAll = new TextRange(textStart, textStart.DocumentEnd.GetInsertionPosition(LogicalDirection.Backward));

			return textRangeAll;
		}
	}
}
