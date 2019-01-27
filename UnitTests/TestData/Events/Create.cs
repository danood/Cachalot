﻿using System;

namespace UnitTests.TestData.Events
{
    public class Create : NegotiatedProductEvent
    {
        
        public override bool NeedsConfirmation => true;

        public override string EventType => "CREATE";

        public Create(int id, string dealId)
        {

            EventId = id;
            
            DealId = dealId;

            EventDate = DateTime.Today;
            ValueDate = DateTime.Today;

        }
    }
}