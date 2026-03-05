document.addEventListener('DOMContentLoaded', function () {
    var importBtn = document.getElementById('importBtn');
    var importResult = document.getElementById('importResult');

    if (importBtn) {
        importBtn.addEventListener('click', async function () {
            importBtn.disabled = true;
            importResult.textContent = 'Importando...';
            try {
                var resp = await fetch('/api/cotacoes/importar', { method: 'POST' });
                if (!resp.ok) {
                    var err = await resp.json().catch(() => ({ error: 'Erro desconhecido' }));
                    importResult.textContent = 'Falha: ' + (err.error || JSON.stringify(err));
                } else {
                    var data = await resp.json();
                    importResult.textContent = 'Importado(s): ' + (data.imported ?? 0);
                }
            } catch (e) {
                importResult.textContent = 'Erro: ' + e.message;
            } finally {
                importBtn.disabled = false;
            }
        });
    }
});
