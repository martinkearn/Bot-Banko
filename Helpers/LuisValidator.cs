using Banko.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Banko.Dialogs.TransferDialog;

namespace Banko.Helpers
{
    public static partial class Validators
    {
        /// <summary>
        /// Check whether each entity is valid and return valid ones in a dictionary.
        /// </summary>
        /// <param name="entities">The LUIS entities from the input arguments.</param>
        /// <returns>A dictionary of the valid entities.</returns>
        public static Dictionary<string, object> LuisValidator(BankoLuisModel._Entities entities)
        {
            var result = new Dictionary<string, object>();

            // Check AccountLabel
            if (entities?.AccountLabel?.Any() is true)
            {
                var accountLabel = entities.AccountLabel.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                if (accountLabel != null)
                {
                    result[Keys.AccountLabel] = accountLabel;
                }
            }

            // Check Money
            if (entities?.money?.Any() is true)
            {
                var number = entities.money.FirstOrDefault().Number;
                if (number != 0.0)
                {
                    // LUIS recognizes numbers as doubles. Convert to decimal.
                    result[Keys.Money] = Convert.ToDecimal(number);
                }
            }

            // Check Date
            if (entities?.datetime?.FirstOrDefault()?.Expressions.Any() is true)
            {
                var candidates = entities.datetime[0].Expressions;
                var resolution = ResolveDate(candidates);

                if (resolution != null)
                {
                    var date = Converters.TimexToDateConverter(resolution.Timex);
                    result[Keys.Date] = date.ToLongDateString();
                }
            }

            return result;
        }
    }
}
