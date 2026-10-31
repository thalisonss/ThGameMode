using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ThGameMode.Utils
{
    /// <summary>
    /// Gerencia hotkeys globais para atalhos rápidos.
    /// Suporta ativação/desativação e alternância de modo.
    /// </summary>
    public static class GlobalHotkeys
    {
        // IDs únicos para cada hotkey
        private const int HOTKEY_TOGGLE = 9001;
        private const int HOTKEY_DETECTMODE = 9002;

        // Chaves pré-definidas (pode ser configurado)
        public static Keys ToggleKey { get; set; } = Keys.Control | Keys.Alt | Keys.G;
        public static Keys DetectModeKey { get; set; } = Keys.Control | Keys.Shift | Keys.G;

        // Callbacks
        public static event Action? ToggleRequested;
        public static event Action? DetectModeRequested;

        private static Form? _ownerForm;
        private static bool _isRegistered = false;

        /// <summary>
        /// Inicializa e registra os hotkeys globais.
        /// </summary>
        public static void Inicializar(Form ownerForm)
        {
            _ownerForm = ownerForm ?? throw new ArgumentNullException(nameof(ownerForm));

            // Substituímos as teclas por valores numéricos para o RegisterHotKey
            var toggleModifier = GetModiferKeys(ToggleKey);
            var toggleVk = GetVirtualKeyCode(ToggleKey);

            var detectModifier = GetModiferKeys(DetectModeKey);
            var detectVk = GetVirtualKeyCode(DetectModeKey);

            _isRegistered = RegisterHotKey(_ownerForm.Handle, HOTKEY_TOGGLE, toggleModifier, toggleVk) &&
                           RegisterHotKey(_ownerForm.Handle, HOTKEY_DETECTMODE, detectModifier, detectVk);

            _ownerForm.FormClosed += (s, e) => Desregistrar();
            _ownerForm.KeyPreview = true;
        }

        /// <summary>
        /// Registra um hotkey global.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        /// <summary>
        /// Desregistra um hotkey global.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Processa mensagens WM_HOTKEY.
        /// </summary>
        public static void ProcessarMensagem(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();

                switch (id)
                {
                    case HOTKEY_TOGGLE:
                        ToggleRequested?.Invoke();
                        break;
                    case HOTKEY_DETECTMODE:
                        DetectModeRequested?.Invoke();
                        break;
                }
            }
        }

        /// <summary>
        /// Remove todos os hotkeys registrados.
        /// </summary>
        public static void Desregistrar()
        {
            if (_isRegistered && _ownerForm != null)
            {
                UnregisterHotKey(_ownerForm.Handle, HOTKEY_TOGGLE);
                UnregisterHotKey(_ownerForm.Handle, HOTKEY_DETECTMODE);
                _isRegistered = false;
                AppLogger.Write(AppLogger.LogLevel.Info, "[Hotkeys] Hotkeys desregistrados");
            }
        }

        /// <summary>
        /// Converte Keys para modificadores do Win32.
        /// </summary>
        private static uint GetModiferKeys(Keys key)
        {
            uint modifiers = 0;

            if ((key & Keys.Control) == Keys.Control)
                modifiers |= 0x0002; // MOD_CONTROL

            if ((key & Keys.Shift) == Keys.Shift)
                modifiers |= 0x0004; // MOD_SHIFT

            if ((key & Keys.Alt) == Keys.Alt)
                modifiers |= 0x0001; // MOD_ALT

            return modifiers;
        }

        /// <summary>
        /// Obtém o código virtual da tecla.
        /// </summary>
        private static uint GetVirtualKeyCode(Keys key)
        {
            // remove os modificadores para obter a tecla principal
            var cleanKey = key & ~(Keys.Control | Keys.Shift | Keys.Alt);

            return (uint)cleanKey;
        }

        /// <summary>
        /// Configura uma nova hotkey para alternância.
        /// </summary>
        public static void ConfigurarToggle(Keys novaKey)
        {
            if (_isRegistered && _ownerForm != null)
            {
                UnregisterHotKey(_ownerForm.Handle, HOTKEY_TOGGLE);
            }

            ToggleKey = novaKey;

            if (_ownerForm != null)
            {
                var modifier = GetModiferKeys(ToggleKey);
                var vk = GetVirtualKeyCode(ToggleKey);
                RegisterHotKey(_ownerForm.Handle, HOTKEY_TOGGLE, modifier, vk);
            }
        }

        /// <summary>
        /// Verifica se os hotkeys estão ativos.
        /// </summary>
        public static bool IsRegistered => _isRegistered;

        /// <summary>
        /// Obtém o nome da hotkey atual.
        /// </summary>
        public static string GetToggleKeyName() => GetKeyName(ToggleKey);

        /// <summary>
        /// Obtém o nome da hotkey de modo.
        /// </summary>
        public static string GetDetectModeKeyName() => GetKeyName(DetectModeKey);

        private static string GetKeyName(Keys key)
        {
            var parts = new List<string>();
            
            if ((key & Keys.Control) == Keys.Control) parts.Add("Ctrl");
            if ((key & Keys.Shift) == Keys.Shift) parts.Add("Shift");
            if ((key & Keys.Alt) == Keys.Alt) parts.Add("Alt");

            var cleanKey = key & ~(Keys.Control | Keys.Shift | Keys.Alt);
            parts.Add(cleanKey.ToString());

            return string.Join(" + ", parts);
        }
    }
}