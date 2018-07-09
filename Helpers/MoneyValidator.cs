using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Banko.Helpers
{
    public static partial class Validators
    {
        public static Task MoneyValidator(ITurnContext context, NumberResult<int> toValidate)
        {
            if (toValidate.Value < 0)
            {
                toValidate.Status = PromptStatus.TooSmall;
            }
            else
            {
                toValidate.Status = PromptStatus.Recognized;
            }

            return Task.CompletedTask;
        }
    }
}
