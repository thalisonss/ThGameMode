using System.Collections.Generic;

namespace ThGameModeService
{
    public class Configuracao
    {
        public int CheckInterval { get; set; } 
        public string PowerPlanOpenApp { get; set; }
        public string PowerPlanClosedApp { get; set; }
        public List<string> ListServices { get; set; } = new();
    }
}
