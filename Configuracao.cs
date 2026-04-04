using System.Collections.Generic;

namespace ThGameMode
{
    public class Configuracao
    {
        public int CheckInterval { get; set; } = 60;
        public string PowerPlanOpenApp { get; set; } = string.Empty;
        public string PowerPlanClosedApp { get; set; } = string.Empty;
        public List<string> ListServices { get; set; } = new();
        public Dictionary<string, string> ManualExecutablePaths { get; set; } = new();
    }
}
