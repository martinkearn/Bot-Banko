using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Prompts;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Banko.Helpers
{
    public static partial class Validators
    {
        /// <summary>
        /// The validatior to use with the reservation date and time prompt.
        /// </summary>
        /// <param name="context">The current turn context.</param>
        /// <param name="toValidate">The input to be validated.</param>
        /// <returns>An updated <paramref name="toValidate"/> value that sets the object's 
        /// Prompt status to indicate whether the value validates.</returns>
        /// <remarks>Valid dates are evenings within the next 2 weeks.</remarks>
        public static Task DateTimeValidator(ITurnContext context, DateTimeResult toValidate)
        {
            if (toValidate.Resolution.Count is 0)
            {
                toValidate.Status = PromptStatus.NotRecognized;
                return Task.CompletedTask;
            }

            var candidates = toValidate.Resolution.Select(res => res.Timex).ToList();

            // Find any matches within dates from this week or next (not in the past), and evenings only.
            var resolution = ResolveDate(candidates);
            if (resolution != null)
            {
                toValidate.Resolution.Clear();
                toValidate.Resolution.Add(resolution);
                toValidate.Status = PromptStatus.Recognized;
            }
            else
            {
                toValidate.Status = PromptStatus.NotRecognized;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Compares a set of candidate date time strings against our validation constraints, and
        /// returns a value that matches; or null, if no candidates meet the constraints.
        /// </summary>
        /// <param name="candidates">The candidate strings.</param>
        /// <returns>A value that matches; or null, if no candidates meet the constraints.</returns>
        /// <remarks>Valid dates are evenings within the next 2 weeks.</remarks>
        private static DateTimeResult.DateTimeResolution ResolveDate(IEnumerable<string> candidates)
        {
            // Find any matches within dates from this week or next (not in the past), and evenings only.
            var constraints = new[]
            {
                TimexCreator.NextWeeksFromToday(2),
                TimexCreator.Evening
            };
            List<TimexProperty> resolutions = null;
            try
            {
                resolutions = TimexRangeResolver.Evaluate(candidates, constraints);
            }
            catch
            {
                return null;
            }

            if (resolutions.Count is 0)
            {
                return null;
            }

            // Use the first recognized value for the reservation time.
            var timex = resolutions[0];
            return new DateTimeResult.DateTimeResolution
            {
                Start = timex.ToNaturalLanguage(DateTime.Now),
                End = timex.ToNaturalLanguage(DateTime.Now),
                Value = timex.ToNaturalLanguage(DateTime.Now),
                Timex = timex.TimexValue
            };
        }
    }
}
