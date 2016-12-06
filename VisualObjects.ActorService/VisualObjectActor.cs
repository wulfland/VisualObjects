// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace VisualObjects.ActorService
{
    using System;
    using System.Threading.Tasks;
    using VisualObjects.Common;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Actors;
    using LaunchDarkly.Client;

    [ActorService(Name = "VisualObjects.ActorService")]
    [StatePersistence(StatePersistence.Persisted)]
    public class VisualObjectActor : Actor, IVisualObjectActor
    {

        private static readonly string StatePropertyName = "VisualObject";
        private IActorTimer updateTimer;
        private string jsonString;
        private LdClient client = new LdClient("sdk-95af6c33-4e12-4532-abde-9ea5a62aef1e");
        private User user = User.WithKey("kaufm");

        public VisualObjectActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        { }

        public Task<string> GetStateAsJsonAsync()
        {
            return Task.FromResult(this.jsonString);
        }

        protected override async Task OnActivateAsync()
        {
            VisualObject newObject = VisualObject.CreateRandom(this.Id.ToString());

            ActorEventSource.Current.ActorMessage(this, "StateCheck {0}", (await this.StateManager.ContainsStateAsync(StatePropertyName)).ToString());

            VisualObject result = await this.StateManager.GetOrAddStateAsync<VisualObject>(StatePropertyName, newObject);

            this.jsonString = result.ToJson();

            // ACTOR MOVEMENT REFRESH
            this.updateTimer = this.RegisterTimer(this.MoveObject, null, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));
            return;
        }

        private async Task MoveObject(object obj)
        {
            VisualObject visualObject = await this.StateManager.GetStateAsync<VisualObject>(StatePropertyName);

            visualObject.Move(false);

            //if (client.BoolVariation("rotate-object-flag", user, false))
            //{
            //    visualObject.Move(true);
            //}
            //else
            //{
            //    visualObject.Move(false);
            //}
            
            await this.StateManager.SetStateAsync<VisualObject>(StatePropertyName, visualObject);

            this.jsonString = visualObject.ToJson();

            return;
        }
    }
}
