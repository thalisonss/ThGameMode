var refreshInterval = null;

function updateAll() {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', '/api/stats', true);
    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4 && xhr.status === 200) {
            try {
                var d = JSON.parse(xhr.responseText);

                // Timestamp e uptime
                if (d.timestamp) document.getElementById('timestamp').textContent = d.timestamp;
                if (d.uptime) document.getElementById('uptime').textContent = d.uptime;

                // IP local
                if (d.local_ip) {
                    var el = document.getElementById('local-ip');
                    if (el) el.textContent = 'http://' + d.local_ip + ':5000';
                }

                // CPU
                document.getElementById('cpu-percent').textContent = d.cpu.percent + '%';
                document.getElementById('cpu-bar').style.width = clamp(d.cpu.percent) + '%';
                document.getElementById('cpu-count').textContent = d.cpu.count;
                document.getElementById('cpu-freq').textContent = Math.round(d.cpu.freq_current / 1000);
                if (d.cpu.temperature !== undefined) document.getElementById('cpu-temp').textContent = d.cpu.temperature;

                // RAM
                document.getElementById('mem-percent').textContent = d.memory.percent + '%';
                document.getElementById('mem-bar').style.width = clamp(d.memory.percent) + '%';
                document.getElementById('mem-used').textContent = d.memory.used;
                document.getElementById('mem-total').textContent = d.memory.total;
                if (d.memory.temperature !== undefined) document.getElementById('mem-temp').textContent = d.memory.temperature;

                // Disco
                document.getElementById('disk-percent').textContent = d.disk.percent + '%';
                document.getElementById('disk-bar').style.width = clamp(d.disk.percent) + '%';
                document.getElementById('disk-used').textContent = d.disk.used;
                document.getElementById('disk-free').textContent = d.disk.free;
                if (d.disk.temperature !== undefined) document.getElementById('disk-temp').textContent = d.disk.temperature;

                // GPU
                document.getElementById('gpu-percent').textContent = d.gpu.percent + '%';
                document.getElementById('gpu-bar').style.width = clamp(d.gpu.percent) + '%';
                if (d.gpu.name) {
                    var gpuEl = document.getElementById('gpu-name');
                    if (gpuEl) gpuEl.textContent = d.gpu.name;
                }
                if (d.gpu.temperature !== undefined) document.getElementById('gpu-temp').textContent = d.gpu.temperature;

                // Placa-Mãe (Múltiplos Sensores)
                if (d.motherboard) {
                    if (d.motherboard.system_temperature !== undefined) {
                        var el = document.getElementById('mb-system-temp');
                        if (el) el.textContent = d.motherboard.system_temperature + '°C';
                    }
                    if (d.motherboard.vrm_temperature !== undefined) {
                        var el = document.getElementById('mb-vrm-temp');
                        if (el) el.textContent = d.motherboard.vrm_temperature + '°C';
                    }
                    if (d.motherboard.pch_temperature !== undefined) {
                        var el = document.getElementById('mb-pch-temp');
                        if (el) el.textContent = d.motherboard.pch_temperature + '°C';
                    }
                    if (d.motherboard.aux_temperature !== undefined) {
                        var el = document.getElementById('mb-aux-temp');
                        if (el) el.textContent = d.motherboard.aux_temperature + '°C';
                    }
                }

                // Consumo de energia
                if (d.power) {
                    var p = d.power;
                    setText('power-total', Math.round(p.total_watts) + ' W');
                    setText('power-cpu', Math.round(p.cpu_watts));
                    setText('power-gpu', Math.round(p.gpu_watts));
                    setText('power-memory', Math.round(p.memory_watts));
                    setText('power-disk', Math.round(p.disk_watts));
                    setText('power-mb', Math.round(p.motherboard_watts));
                    setText('power-fans', Math.round(p.fans_watts));
                    setText('power-other', Math.round(p.peripherals_watts));
                    setText('power-wall', Math.round(p.wall_watts));
                    setText('power-session', Number(p.session_kwh || 0).toFixed(3));
                    setText('power-total-energy', Number(p.total_kwh || 0).toFixed(3));
                }

                // Clima
                if (d.weather && d.weather.temp !== undefined) {
                    document.getElementById('weather-temp').textContent = d.weather.temp + '°C';
                }
            } catch (e) {
                console.error('Erro ao parsear stats:', e);
            }
        }
    };
    xhr.send();
}

function setText(id, value) {
    var el = document.getElementById(id);
    if (el) el.textContent = value;
}

function clamp(v) {
    return Math.max(0, Math.min(100, v || 0));
}

// Atualiza a cada 2 segundos (igual ao backend)
updateAll();
refreshInterval = setInterval(updateAll, 2000);
