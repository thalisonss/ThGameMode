using System.Collections.Generic;

namespace ThGameMode
{
    /// <summary>
    /// Modelo serializado em JSON com todas as preferências persistidas pelo usuário.
    /// </summary>
    public class Configuracao
    {
        /// <summary>
        /// Intervalo, em segundos, entre cada varredura de processos/serviços monitorados.
        /// </summary>
        public int CheckInterval { get; set; } = 60;

        /// <summary>
        /// GUID do plano de energia que deve ser ativado quando algum item monitorado estiver em execução.
        /// </summary>
        public string PowerPlanOpenApp { get; set; } = string.Empty;

        /// <summary>
        /// GUID do plano de energia que deve ser ativado quando nenhum item monitorado estiver em execução.
        /// </summary>
        public string PowerPlanClosedApp { get; set; } = string.Empty;

        /// <summary>
        /// Lista de serviços ou processos que disparam a troca de modo.
        /// </summary>
        public List<string> ListServices { get; set; } = new();
    }
}
