using System;
using System.Text.RegularExpressions;

namespace Terraria.Localization;

public partial class LocalizedText
{
	private bool? _hasPlurals;

	// https://www.unicode.org/cldr/charts/43/supplemental/language_plural_rules.html
	// implementations extracted from build of https://github.com/xyzsd/cldr-plural-rules
	// English, German, Italian, Spanish, Portugese, French
	//   one, other
	// Russian, Polish
	//   one, few, many
	// Chinese
	//   other
	public static int CardinalPluralRule(GameCulture culture, int count)
	{
		int mod_i10 = count % 10;
		int mod_i100 = count % 100;
		static bool contains(int i, int a, int b) => i >= a && i <= b;

		switch (culture.LegacyId) {
			case (int)GameCulture.CultureName.Russian:
				// one, few, many
				// _C_RULE_13
				if (mod_i10 == 1 && mod_i100 != 11)
					return 0;

				if (contains(mod_i10, 2, 4) && !contains(mod_i100, 12, 14))
					return 1;

				return 2;

			case (int)GameCulture.CultureName.English:
			case (int)GameCulture.CultureName.German:
			case (int)GameCulture.CultureName.Italian:
			case (int)GameCulture.CultureName.Spanish:
			case (int)GameCulture.CultureName.Portuguese:
				// one, many
				return count == 1 ? 0 : 1;

			case (int)GameCulture.CultureName.French:
				// one, many
				return count == 0 || count == 1 ? 0 : 1;

			case (int)GameCulture.CultureName.Polish:
				// one, few, many
				if (count == 1)
					return 0;
				
				if (contains(mod_i10, 2, 4) && !contains(mod_i100, 12, 14))
					return 1;
				
				return 2;

			case (int)GameCulture.CultureName.Chinese:
			default:
				// Chinese
				// other
				return 0;
		}
	}

	public static readonly Regex PluralizationPatternRegex = new Regex(@"{\^(\d+):([^\r\n]+?)}", RegexOptions.Compiled); // "{0} {^0:item;items}"

	public static string ApplyPluralization(string value, params object[] args)
	{
		return PluralizationPatternRegex.Replace(value, delegate (Match match) {
			int argIndex = Convert.ToInt32(match.Groups[1].Value);
			string[] options = match.Groups[2].Value.Split(';');
			int count = Convert.ToInt32(args[argIndex]);			
			int rule = CardinalPluralRule(Language.ActiveCulture, count);
			return options[Math.Min(rule, options.Length-1)];
		});
	}

	public string Format(params object[] args)
	{
		string value = Value;
		if (_hasPlurals ??= PluralizationPatternRegex.IsMatch(value))
			value = ApplyPluralization(value, args);

		return string.Format(value, args);
	}

	/// <summary>
	/// Creates a new LocalizedText with the supplied arguments formatted into the value (via <see cref="string.Format(string, object?[])"/>)<br/>
	/// Will automatically update to re-format the string with cached args when language changes. <br/>
	///<br/>
	/// The resulting LocalizedText should be stored statically. Should not be used to create 'throwaway' LocalizedText instances. <br/>
	/// Use <see cref="Format(object[])"/> instead for repeated on-demand formatting with different args.
	/// </summary>
	/// <param name="args">The substitution args</param>
	/// <returns></returns>
	public LocalizedText WithFormatArgs(params object[] args) => LanguageManager.Instance.BindFormatArgs(Key, args);
}
