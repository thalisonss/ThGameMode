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

                // RAM
                document.getElementById('mem-percent').textContent = d.memory.percent + '%';
                document.getElementById('mem-bar').style.width = clamp(d.memory.percent) + '%';
                document.getElementById('mem-used').textContent = d.memory.used;
                document.getElementById('mem-total').textContent = d.memory.total;

                // Disco
                document.getElementById('disk-percent').textContent = d.disk.percent + '%';
                document.getElementById('disk-bar').style.width = clamp(d.disk.percent) + '%';
                document.getElementById('disk-used').textContent = d.disk.used;
                document.getElementById('disk-free').textContent = d.disk.free;
                document.getElementById('disk-total').textContent = d.disk.total;

                // GPU
                document.getElementById('gpu-percent').textContent = d.gpu.percent + '%';
                document.getElementById('gpu-bar').style.width = clamp(d.gpu.percent) + '%';
                if (d.gpu.name) {
                    var gpuEl = document.getElementById('gpu-name');
                    if (gpuEl) gpuEl.textContent = d.gpu.name;
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

function clamp(v) {
    return Math.max(0, Math.min(100, v || 0));
}

// Atualiza a cada 2 segundos (igual ao backend)
updateAll();
refreshInterval = setInterval(updateAll, 2000);
