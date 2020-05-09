﻿using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using TombLib.Scripting.Resources;
using TombLib.Scripting.TextEditors.Configs;

namespace TombLib.Scripting.TextEditors.SyntaxHighlighting
{
	public sealed class ScriptSyntaxHighlighting : IHighlightingDefinition
	{
		private readonly ClassicScriptEditorConfiguration _config = new ClassicScriptEditorConfiguration().Load<ClassicScriptEditorConfiguration>();

		public string Name { get { return "Script Rules"; } }

		public HighlightingRuleSet MainRuleSet
		{
			get
			{
				HighlightingRuleSet ruleSet = new HighlightingRuleSet();

				/* Comments */
				ruleSet.Rules.Add(new HighlightingRule
				{
					Regex = new Regex(ScriptPatterns.Comments),
					Color = new HighlightingColor
					{
						Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(_config.ColorScheme.Comments))
					}
				});

				/* Sections */
				ruleSet.Rules.Add(new HighlightingRule
				{
					Regex = new Regex(ScriptPatterns.Sections, RegexOptions.IgnoreCase),
					Color = new HighlightingColor
					{
						Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(_config.ColorScheme.Sections)),
						FontWeight = FontWeights.Bold
					}
				});

				/* Standard commands */
				ruleSet.Rules.Add(new HighlightingRule
				{
					Regex = new Regex(ScriptPatterns.StandardCommands, RegexOptions.IgnoreCase),
					Color = new HighlightingColor
					{
						Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(_config.ColorScheme.StandardCommands))
					}
				});

				/* New commands */
				ruleSet.Rules.Add(new HighlightingRule
				{
					Regex = new Regex(ScriptPatterns.NewCommands, RegexOptions.IgnoreCase),
					Color = new HighlightingColor
					{
						Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(_config.ColorScheme.NewCommands))
					}
				});

				/* Next line keys */
				ruleSet.Rules.Add(new HighlightingRule
				{
					Regex = new Regex(ScriptPatterns.NextLineKey),
					Color = new HighlightingColor
					{
						Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(_config.ColorScheme.NewCommands)),
						FontWeight = FontWeights.Bold
					}
				});

				/* Commas */
				ruleSet.Rules.Add(new HighlightingRule
				{
					Regex = new Regex(ScriptPatterns.Comma),
					Color = new HighlightingColor
					{
						Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(_config.ColorScheme.Foreground)),
						FontWeight = FontWeights.Bold
					}
				});

				/* Mnemonics */
				ruleSet.Rules.Add(new HighlightingRule
				{
					Regex = new Regex(ScriptPatterns.Mnemonics, RegexOptions.IgnoreCase),
					Color = new HighlightingColor
					{
						Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(_config.ColorScheme.References))
					}
				});

				/* Hex values */
				ruleSet.Rules.Add(new HighlightingRule
				{
					Regex = new Regex(ScriptPatterns.HexValues, RegexOptions.IgnoreCase),
					Color = new HighlightingColor
					{
						Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(_config.ColorScheme.References))
					}
				});

				/* Directives (#...) */
				ruleSet.Rules.Add(new HighlightingRule
				{
					Regex = new Regex(ScriptPatterns.Directives, RegexOptions.IgnoreCase),
					Color = new HighlightingColor
					{
						Foreground = new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(_config.ColorScheme.References))
					}
				});

				ruleSet.Name = "Script Rules";
				return ruleSet;
			}
		}

		public IEnumerable<HighlightingColor> NamedHighlightingColors { get { throw new NotImplementedException(); } }
		public IDictionary<string, string> Properties { get { throw new NotImplementedException(); } }

		public HighlightingColor GetNamedColor(string name)
		{
			throw new NotImplementedException();
		}

		public HighlightingRuleSet GetNamedRuleSet(string name)
		{
			throw new NotImplementedException();
		}
	}
}
