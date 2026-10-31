using System;
using System.IO;
using System.Text.Json;

namespace ThGameMode.Utils
{
    /// <summary>
    /// Gerencia backups automáticos das configurações do ThGameMode.
    /// </summary>
    public static class ConfigBackupManager
    {
        private static readonly string BackupDirectory = Path.Combine(AppContext.BaseDirectory, "backups");
        private const int MaxBackups = 10;

        /// <summary>
        /// Cria um backup da configuração atual.
        /// </summary>
        public static bool CriarBackup(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    AppLogger.Write(AppLogger.LogLevel.Warning, "[Backup] Arquivo de configuração não existe, backup não criado");
                    return false;
                }

                // Garante que o diretório existe
                Directory.CreateDirectory(BackupDirectory);

                // Nome do backup com timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"config_backup_{timestamp}.json";
                var backupPath = Path.Combine(BackupDirectory, backupFileName);

                // Copia o arquivo
                File.Copy(configPath, backupPath, overwrite: true);

                AppLogger.Write(AppLogger.LogLevel.Info, $"[Backup] Criado: {backupFileName}");

                // Limpa backups antigos
                LimparBackupsAntigos();

                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, $"[Backup] Erro ao criar backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove backups antigos mantendo apenas os mais recentes.
        /// </summary>
        private static void LimparBackupsAntigos()
        {
            try
            {
                if (!Directory.Exists(BackupDirectory))
                    return;

                var backups = Directory.GetFiles(BackupDirectory, "config_backup_*.json")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // Remove backups excedentes
                for (int i = MaxBackups; i < backups.Count; i++)
                {
                    try
                    {
                        backups[i].Delete();
                        AppLogger.Write(AppLogger.LogLevel.Info, $"[Backup] Removido backup antigo: {backups[i].Name}");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Write(AppLogger.LogLevel.Warning, $"[Backup] Erro ao remover backup antigo: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, $"[Backup] Erro ao limpar backups: {ex.Message}");
            }
        }

        /// <summary>
        /// Lista todos os backups disponíveis.
        /// </summary>
        public static List<BackupInfo> ListarBackups()
        {
            var lista = new List<BackupInfo>();

            try
            {
                if (!Directory.Exists(BackupDirectory))
                    return lista;

                var backups = Directory.GetFiles(BackupDirectory, "config_backup_*.json")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime);

                foreach (var backup in backups)
                {
                    lista.Add(new BackupInfo
                    {
                        NomeArquivo = backup.Name,
                        CaminhoCompleto = backup.FullName,
                        DataCriacao = backup.CreationTime,
                        Tamanho = backup.Length
                    });
                }
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, $"[Backup] Erro ao listar backups: {ex.Message}");
            }

            return lista;
        }

        /// <summary>
        /// Restaura uma configuração a partir de um backup.
        /// </summary>
        public static bool RestaurarBackup(string backupPath, string configPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    AppLogger.Write(AppLogger.LogLevel.Error, "[Backup] Arquivo de backup não encontrado");
                    return false;
                }

                // Cria backup do arquivo atual antes de sobrescrever
                if (File.Exists(configPath))
                {
                    var tempBackup = configPath + ".before_restore";
                    File.Copy(configPath, tempBackup, overwrite: true);
                }

                // Restaura o backup
                File.Copy(backupPath, configPath, overwrite: true);

                AppLogger.Write(AppLogger.LogLevel.Info, $"[Backup] Configuração restaurada de: {Path.GetFileName(backupPath)}");
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, $"[Backup] Erro ao restaurar backup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Abre o explorador de arquivos na pasta de backups.
        /// </summary>
        public static void AbrirPastaBackups()
        {
            try
            {
                Directory.CreateDirectory(BackupDirectory);
                System.Diagnostics.Process.Start("explorer.exe", BackupDirectory);
            }
            catch (Exception ex)
            {
                AppLogger.Write(AppLogger.LogLevel.Error, $"[Backup] Erro ao abrir pasta de backups: {ex.Message}");
            }
        }

        public class BackupInfo
        {
            public string NomeArquivo { get; set; } = string.Empty;
            public string CaminhoCompleto { get; set; } = string.Empty;
            public DateTime DataCriacao { get; set; }
            public long Tamanho { get; set; }

            public string TamanhoFormatado => Tamanho < 1024
                ? $"{Tamanho} bytes"
                : $"{Tamanho / 1024.0:F1} KB";
        }
    }
}
