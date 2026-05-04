namespace World
{
    public readonly struct PlacementResult
    {
        public bool Success { get; }
        public PlacementFailReason Reason { get; }
        public string Message { get; }

        private PlacementResult(bool success, PlacementFailReason reason, string message)
        {
            Success = success;
            Reason = reason;
            Message = message;
        }

        public static PlacementResult Ok()
        {
            return new PlacementResult(
                true,
                PlacementFailReason.None,
                string.Empty
            );
        }

        public static PlacementResult Fail(PlacementFailReason reason, string message = "")
        {
            return new PlacementResult(
                false,
                reason,
                message
            );
        }

        public override string ToString()
        {
            if (Success)
                return "Placement successful.";

            if (string.IsNullOrEmpty(Message))
                return $"Placement failed: {Reason}";

            return $"Placement failed: {Reason}. {Message}";
        }
    }
}

