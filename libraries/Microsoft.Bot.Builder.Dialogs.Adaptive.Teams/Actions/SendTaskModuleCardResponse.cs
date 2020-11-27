﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Actions
{
    /// <summary>
    /// Send an Card Task Module Continue response to the user.
    /// </summary>
    public class SendTaskModuleCardResponse : BaseSendTaskModuleContinueResponse
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "Teams.SendTaskModuleCardResponse";

        /// <summary>
        /// Initializes a new instance of the <see cref="SendTaskModuleCardResponse"/> class.
        /// </summary>
        /// <param name="callerPath">Optional, source file full path.</param>
        /// <param name="callerLine">Optional, line number in source file.</param>
        [JsonConstructor]
        public SendTaskModuleCardResponse([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets or sets template for the activity expression containing a Hero Card or Adaptive Card with an Attachment to send.
        /// </summary>
        /// <value>
        /// Template for the activity.
        /// </value>
        [JsonProperty("activity")]
        public ITemplate<Activity> Activity { get; set; }

        /// <summary>
        /// Called when the dialog is started and pushed onto the dialog stack.
        /// </summary>
        /// <param name="dc">The <see cref="DialogContext"/> for the current turn of conversation.</param>
        /// <param name="options">Optional, initial information to pass to the dialog.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            if (this.Disabled != null && this.Disabled.GetValue(dc.State) == true)
            {
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            Attachment attachment = null;
            if (Activity != null)
            {
                var boundActivity = await Activity.BindAsync(dc, dc.State).ConfigureAwait(false);

                if (boundActivity.Attachments == null || !boundActivity.Attachments.Any())
                {
                    throw new ArgumentException($"Invalid Activity. The activity does not contain a valid attachment as required for Task Module Continue Response.");
                }

                attachment = boundActivity.Attachments[0];
            }
            else
            {
                throw new ArgumentException($"A valid card attachment is required for Task Module Continue Response.");
            }

            var title = Title?.GetValue(dc.State);
            var height = Height?.GetValue(dc.State);
            var width = Width?.GetValue(dc.State);
            
            var completionBotId = CompletionBotId?.GetValue(dc.State);

            var response = new TaskModuleResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Value = new TaskModuleTaskInfo
                    {
                        Title = title,
                        Card = attachment,
                        Height = height,
                        Width = width,
                        CompletionBotId = completionBotId,
                    },
                },
                CacheInfo = GetCacheInfo(dc),
            };

            var responseActivity = CreateInvokeResponseActivity(response);
            ResourceResponse sendResponse = await dc.Context.SendActivityAsync(responseActivity, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await dc.EndDialogAsync(sendResponse, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Builds the compute Id for the dialog.
        /// </summary>
        /// <returns>A string representing the compute Id.</returns>
        protected override string OnComputeId()
        {
            if (Activity is ActivityTemplate at)
            {
                return $"{this.GetType().Name}({StringUtils.Ellipsis(at.Template.Trim(), 30)})";
            }

            return $"{this.GetType().Name}('{StringUtils.Ellipsis(Activity?.ToString().Trim(), 30)}')";
        }
    }
}