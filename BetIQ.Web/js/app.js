// ============================================================================
// BetIQ App - Lógica Principal (Frontend)
// ============================================================================

// [1] Configuraciones Principales
// Conexión base hacia la API Backend en C# (.NET Core)
const API_BASE_URL = 'http://localhost:5023/api'; 

// [2] Elementos del DOM (Interfaz Gráfica)
const localSelect = document.getElementById('localSelect');
const visitorSelect = document.getElementById('visitorSelect');
const btnCalculate = document.getElementById('btnCalculate');
const rankingBody = document.getElementById('rankingBody');

// Componentes de la pantalla de resultados del simulador
const predictionResult = document.getElementById('predictionResult');
const winnerText = document.getElementById('winnerText');
const evAlerts = document.getElementById('evAlerts'); // Contenedor para Alertas de Valor Esperado (EV)

// Inputs de usuario para cuotas y gestión de dinero (Bankroll)
const localOddsInput = document.getElementById('localOdds');
const visitorOddsInput = document.getElementById('visitorOdds');
const bankrollInput = document.getElementById('bankrollInput');

// Selector de Deporte Global (NBA, Fútbol, Tenis)
const sportContext = document.getElementById('sportContext');

// Variables de Estado
let allTeams = []; // Almacena todos los equipos cargados desde la API
let currentSport = 'NBA'; // Deporte activo por defecto
let currentRankingPage = 1; // Página actual para la tabla de posiciones ELO
const itemsPerPageRanking = 10; // Cantidad de equipos mostrados por página en el ranking

// [3] Inicialización de la Aplicación
// Se ejecuta cuando el HTML ha cargado completamente
document.addEventListener('DOMContentLoaded', () => {
    initApp();
    setupEventListeners();
});

// Función principal de arranque
async function initApp() {
    currentSport = sportContext.value;
    updateUIForSport();
    makePanelsCollapsible();
    
    // Mostrar/ocultar selector de liga según el deporte activo
    const ligaWrap = document.getElementById('ligaSelectorWrap');
    if (ligaWrap) ligaWrap.style.display = currentSport === 'Futbol' ? 'block' : 'none';

    // Mostrar/ocultar selector de superficie según el deporte activo
    const surfaceWrap = document.getElementById('surfaceSelectorWrap');
    if (surfaceWrap) surfaceWrap.style.display = currentSport === 'Tenis' ? 'block' : 'none';

    loadTeams();
    loadValueBets();
    loadMatches();
    loadStandings();
    
    // Si cambiamos a Fútbol, cargamos las ligas disponibles
    if (currentSport === 'Futbol') loadFutbolLigas();
}

function setupEventListeners() {
    sportContext.addEventListener('change', () => { initApp(); });
    localSelect.addEventListener('change', validateSelection);
    visitorSelect.addEventListener('change', validateSelection);
    btnCalculate.addEventListener('click', calculateProbability);
    bankrollInput.addEventListener('input', () => { loadValueBets(); });
    
    // Filtrar partidos al cambiar de liga
    const ligaSelector = document.getElementById('ligaSelector');
    if (ligaSelector) {
        ligaSelector.addEventListener('change', () => { loadFutbolMatches(); });
    }
}

function updateUIForSport() {
    const titles = {
        'NBA': { events: 'Partidos NBA de Hoy', rankings: 'Posiciones NBA' },
        'Futbol': { events: 'Partidos de Fútbol', rankings: 'Ranking Fútbol' },
        'Tenis': { events: 'Torneos de Tenis', rankings: 'Ranking ATP/WTA' }
    };
    
    const eventsHeader = document.querySelector('#nbaMatches .panel-header h2');
    if (eventsHeader) eventsHeader.innerHTML = `
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="color: #ff9f43;"><circle cx="12" cy="12" r="10"/><path d="M12 2a14.5 14.5 0 0 0 0 20"/><path d="M2.05 12h19.9"/><path d="M12 22A14.5 14.5 0 0 0 12 2"/></svg>
        ${titles[currentSport].events}
    `;
    
    // Busca tanto .panel-header como .section-header para compatibilidad
    const standingsHeader = document.querySelector('#posiciones .panel-header h2') || document.querySelector('#posiciones .section-header h2');
    if (standingsHeader) standingsHeader.innerHTML = `
        <span class="icon">🏆</span> ${titles[currentSport].rankings}
    `;
}

// [4] Lógica del Simulador ELO
// Habilita el botón de cálculo solo si se han seleccionado dos equipos diferentes
function validateSelection() {
    const local = localSelect.value;
    const visitor = visitorSelect.value;
    
    if (local && visitor && local !== visitor) {
        btnCalculate.disabled = false;
    } else {
        btnCalculate.disabled = true;
    }
}

// Carga de Equipos Globales (Para los Selects y el Ranking)
async function loadTeams() {
    try {
        // Llama a la API (Controlador TeamsController) para traer la lista global
        const response = await fetch(`${API_BASE_URL}/teams`);
        if (!response.ok) throw new Error('Error en la red');
        
        let teams = await response.json();
        
        // Lista de franquicias oficiales de la NBA
        const nbaOfficialTeams = [
            "Atlanta Hawks", "Boston Celtics", "Brooklyn Nets", "Charlotte Hornets", "Chicago Bulls", 
            "Cleveland Cavaliers", "Dallas Mavericks", "Denver Nuggets", "Detroit Pistons", "Golden State Warriors", 
            "Houston Rockets", "Indiana Pacers", "LA Clippers", "Los Angeles Lakers", "Memphis Grizzlies", 
            "Miami Heat", "Milwaukee Bucks", "Minnesota Timberwolves", "New Orleans Pelicans", "New York Knicks", 
            "Oklahoma City Thunder", "Orlando Magic", "Philadelphia 76ers", "Phoenix Suns", "Portland Trail Blazers", 
            "Sacramento Kings", "San Antonio Spurs", "Toronto Raptors", "Utah Jazz", "Washington Wizards"
        ];

        // Filtra los equipos para mostrar solo los correspondientes al deporte actual (NBA, Tenis, etc.)
        allTeams = teams.filter(t => {
            if (currentSport === 'NBA') {
                return t.deporte === 'NBA' && nbaOfficialTeams.includes(t.nombreEquipo);
            }
            return t.deporte === currentSport;
        });
        
        // Ordena los equipos por su puntuación ELO (De mayor a menor)
        allTeams.sort((a, b) => b.eloActual - a.eloActual);
        
        populateSelects(); // Llena los menús desplegables
        renderRankingTable(); // Dibuja la tabla ELO
    } catch (error) {
        console.error("Error cargando equipos:", error);
        rankingBody.innerHTML = `<tr><td colspan="4" class="text-center" style="color: #ef4444;">Error al cargar datos. ¿Está la API encendida?</td></tr>`;
    }
}

function populateSelects() {
    // Limpiar opciones previas
    localSelect.innerHTML = '<option value="">Seleccione Equipo Local...</option>';
    visitorSelect.innerHTML = '<option value="">Seleccione Equipo Visitante...</option>';
    
    // Rellenamos ordenado alfabéticamente para facilitar la busqueda
    const sortedByName = [...allTeams].sort((a, b) => a.nombreEquipo.localeCompare(b.nombreEquipo));
    
    sortedByName.forEach(team => {
        const option1 = document.createElement('option');
        option1.value = team.nombreEquipo;
        option1.textContent = team.nombreEquipo;
        
        const option2 = document.createElement('option');
        option2.value = team.nombreEquipo;
        option2.textContent = team.nombreEquipo;
        
        localSelect.appendChild(option1);
        visitorSelect.appendChild(option2);
    });
}

function renderRankingTable() {
    rankingBody.innerHTML = '';
    
    const totalPages = Math.ceil(allTeams.length / itemsPerPageRanking);
    if (currentRankingPage > totalPages && totalPages > 0) {
        currentRankingPage = totalPages;
    }
    
    const startIndex = (currentRankingPage - 1) * itemsPerPageRanking;
    const endIndex = startIndex + itemsPerPageRanking;
    const paginatedTeams = allTeams.slice(startIndex, endIndex);
    
    paginatedTeams.forEach((team, idx) => {
        const tr = document.createElement('tr');
        const realIndex = startIndex + idx;
        
        // Destaque visual a los primeros 3
        if (realIndex < 3) tr.style.fontWeight = 'bold';
        
        tr.innerHTML = `
            <td>#${realIndex + 1}</td>
            <td>${team.nombreEquipo}</td>
            <td style="color: var(--accent-secondary); font-weight: bold;">${team.eloActual}</td>
            <td>${team.deporte}</td>
        `;
        rankingBody.appendChild(tr);
    });

    renderRankingPagination(totalPages);
}

function renderRankingPagination(totalPages) {
    let paginationDiv = document.getElementById('rankingPagination');
    if (!paginationDiv) return;
    
    paginationDiv.innerHTML = '';
    
    if (totalPages <= 1) return;
    
    const btnPrev = document.createElement('button');
    btnPrev.textContent = '◀ Ant';
    btnPrev.className = 'btn-page';
    btnPrev.disabled = currentRankingPage === 1;
    btnPrev.onclick = () => {
        if (currentRankingPage > 1) {
            currentRankingPage--;
            renderRankingTable();
        }
    };
    paginationDiv.appendChild(btnPrev);
    
    const pageInfo = document.createElement('span');
    pageInfo.textContent = `Página ${currentRankingPage} de ${totalPages}`;
    pageInfo.className = 'page-info';
    paginationDiv.appendChild(pageInfo);
    
    const btnNext = document.createElement('button');
    btnNext.textContent = 'Sig ▶';
    btnNext.className = 'btn-page';
    btnNext.disabled = currentRankingPage === totalPages;
    btnNext.onclick = () => {
        if (currentRankingPage < totalPages) {
            currentRankingPage++;
            renderRankingTable();
        }
    };
    paginationDiv.appendChild(btnNext);
}

// [5] Cálculo de Probabilidades y Kelly Criterion Manual (Simulador)
async function calculateProbability() {
    // Lee los valores de los inputs del usuario (Equipos y Cuotas del mercado)
    const local = localSelect.value;
    const visitor = visitorSelect.value;
    const cuotaLocal = localOddsInput.value || 1.0;
    const cuotaVisita = visitorOddsInput.value || 1.0;
    
    btnCalculate.disabled = true;
    btnCalculate.textContent = "Calculando...";
    
    try {
        let apiUrl = `${API_BASE_URL}/teams/${encodeURIComponent(local)}/ev-vs/${encodeURIComponent(visitor)}?cuotaLocal=${cuotaLocal}&cuotaVisita=${cuotaVisita}&sport=${currentSport}`;
        
        const surfaceWrap = document.getElementById('surfaceSelectorWrap');
        const surfaceSelect = document.getElementById('surfaceSelector');
        if (currentSport === 'Tenis' && surfaceWrap && surfaceWrap.style.display !== 'none' && surfaceSelect) {
            apiUrl += `&superficie=${encodeURIComponent(surfaceSelect.value)}`;
        }

        const response = await fetch(apiUrl);
        if (!response.ok) throw new Error('Error en la API');
        
        const data = await response.json();
        
        // Mostrar mensaje final de ganador matemático
        predictionResult.classList.remove('hidden');
        predictionResult.classList.add('active');
        
        const pLocal = data.local.probabilidadVictoria;
        const pVisitor = data.visitante.probabilidadVictoria;
        
        if (pLocal > pVisitor) {
            winnerText.innerHTML = `🌟 <strong>${local}</strong> ganará.`;
        } else if (pVisitor > pLocal) {
            winnerText.innerHTML = `🌟 <strong>${visitor}</strong> ganará.`;
        } else {
            winnerText.innerHTML = `Empate técnico.`;
        }

        // Análisis de Rentabilidad
        evAlerts.innerHTML = '';
        const bankroll = parseFloat(bankrollInput.value) || 0;

        const renderAlert = (teamData, odds) => {
            const margin = (teamData.expectedValue * 100).toFixed(2);
            if (teamData.valueBet || teamData.isValueBet) {
                let kellyHtml = "";
                if (teamData.porcentajeKelly > 0 && bankroll > 0) {
                    const stake = (bankroll * teamData.porcentajeKelly).toFixed(2);
                    kellyHtml = `<div style="margin-top:10px;color:var(--success);">Sugerencia: $${stake}</div>`;
                }
                return `<div style="padding:15px; border-radius:8px; border:1px solid var(--success); background:var(--success-glow);">
                            <span style="color:var(--success); font-weight:bold;">✅ VALUE BET (+${margin}%)</span>
                            ${kellyHtml}
                        </div>`;
            } else {
                return `<div style="padding:15px; border-radius:8px; border:1px solid var(--danger); background:var(--danger-glow);">
                            <span style="color:var(--danger); font-weight:bold;">🚫 EV NEGATIVO (${margin}%)</span>
                        </div>`;
            }
        };
                               
        evAlerts.innerHTML += `<div style="flex:1;">
                                   <h4 style="margin-bottom:10px;">${local}</h4>
                                   ${renderAlert(data.local, cuotaLocal)}
                               </div>`;
                               
        evAlerts.innerHTML += `<div style="flex:1;">
                                   <h4 style="margin-bottom:10px;">${visitor}</h4>
                                   ${renderAlert(data.visitante, cuotaVisita)}
                               </div>`;
        
    } catch (error) {
        console.error("Error calculando:", error);
    } finally {
        btnCalculate.disabled = false;
        btnCalculate.textContent = "PROYECTAR";
    }
}

// ============================================================================
// [6] Oportunidades Automáticas del Mercado (Radar Value Bets)
// ============================================================================
async function loadValueBets() {
    const container = document.getElementById('valueBetsContainer');
    const dateLabel = document.getElementById('dateOfAnalysis');
    
    try {
        // Llama a la API (Controlador TeamsController -> GET /api/analysis/value-bets)
        // Pedimos Value Bets de los próximos días (sin pasar un date específico los trae todos)
        const response = await fetch(`${API_BASE_URL}/analysis/value-bets?sport=${currentSport}`);
        if (!response.ok) throw new Error('Network error');
        
        const data = await response.json();
        dateLabel.textContent = `Analizando para: ${data.analisis_Para} | ${data.total_ValueBets_Encontradas} hallazgos`;
        
        container.innerHTML = '';
        
        if (!data.oportunidades || data.oportunidades.length === 0) {
            container.innerHTML = `
                <div style="padding: 2rem; text-align: center; color: var(--text-muted); grid-column: 1 / -1;">
                    <i class="fas fa-search" style="font-size: 2rem; margin-bottom: 1rem; opacity: 0.5;"></i>
                    <p>No se encontraron partidos próximos con cuotas de mercado activas.</p>
                    <p style="font-size: 0.9em; opacity: 0.7;">Intenta nuevamente más tarde o carga los datos del casino nuevamente.</p>
                </div>`;
            return;
        }

        data.oportunidades.forEach(match => {
            // Buscamos cuál de los dos lados es el "Value Bet"
            const betTarget = match.local.valueBet ? match.local : match.visitante;
            const betAgainst = match.local.valueBet ? match.visitante : match.local;
            
            // Si el EV es muy alto (+1.0), le damos borde infinito
            const isHighValue = betTarget.ev > 0.8; 

            // Formatear Fecha y Hora al horario de Chile (CLT/CLST)
            const eventDate = new Date(match.fecha);
            const chileTimeOptions = { 
                timeZone: 'America/Santiago', 
                hour: '2-digit', 
                minute: '2-digit',
                hour12: false
            };
            const timeString = eventDate.toLocaleTimeString('es-CL', chileTimeOptions);
            const dateString = eventDate.toLocaleDateString('es-CL', { timeZone: 'America/Santiago', month: 'short', day: 'numeric' });
            
            const card = document.createElement('div');
            card.className = `vb-card ${isHighValue ? 'high-value' : ''}`;
            
            card.innerHTML = `
                <div class="vb-content">
                    <div style="display:flex; justify-content:space-between; align-items:flex-start; margin-bottom: 1rem;">
                        <span style="background:var(--success-glow); color:var(--success); padding:4px 8px; border-radius:4px; font-size:0.8rem; font-weight:800;">
                            EV+ ${betTarget.ev}
                        </span>
                        <div style="text-align: right;">
                            <span style="display:block; color:var(--accent-secondary); font-size:0.75rem; font-weight:600;">
                                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:inline-block; margin-right:2px;"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                                ${dateString} - ${timeString}
                            </span>
                            <span style="color:var(--text-muted); font-size:0.7rem;">
                                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:inline-block; margin-right:2px;"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
                                ${match.estadio}
                            </span>
                        </div>
                    </div>
                    
                    <div class="vb-teams">
                        <span style="color:var(--accent-secondary);">${betTarget.equipo}</span>
                        <span style="color:var(--text-muted); font-size:0.9rem; margin:0 5px;">vs</span>
                        <span style="color:var(--text-muted);">${betAgainst.equipo}</span>
                    </div>

                    <div class="vb-metrics">
                        <div class="metric-box">
                            <span class="metric-label">Cuota</span>
                            <span class="metric-value">x${betTarget.cuota_Mercado}</span>
                        </div>
                        <div class="metric-box">
                            <span class="metric-label">Prob. ELO</span>
                            <span class="metric-value">${betTarget.probabilidad_Real}</span>
                        </div>
                    </div>

                    <div style="margin-top: 1.5rem; border-top: 1px dashed var(--border-glass); padding-top: 1rem;">
                        <span class="metric-label" style="display:block; margin-bottom:5px;">KELLY CRITERION</span>
                        <span class="metric-value positive" style="font-size:1rem;">
                            <!-- Convertimos a CLP usando la fórmula base si sugiere apostar -->
                            ${ 
                                (() => {
                                    if (betTarget.sugerencia_Kelly === "NO APOSTAR") return "NO APOSTAR";
                                    const bankroll = parseFloat(bankrollInput.value) || 1000;
                                    const percentageStr = betTarget.sugerencia_Kelly.replace('% del Bankroll', '');
                                    const percentageFloat = parseFloat(percentageStr) / 100;
                                    const amountToBet = bankroll * percentageFloat;
                                    return new Intl.NumberFormat('es-CL', { style: 'currency', currency: 'CLP' }).format(amountToBet);
                                })()
                            }
                        </span>
                    </div>
                </div>
            `;
            container.appendChild(card);
        });

    } catch (error) {
        container.innerHTML = '<p style="color:var(--danger); padding:2rem;">API desconectada. Corre <kbd>dotnet run</kbd> en BetIQ.API.</p>';
        dateLabel.textContent = 'Error de Conexión';
    }
}

// ============================================================================
// [7] Carga de Eventos del Día (Partidos programados)
// ============================================================================
// Selector de carga de eventos según el deporte activo
async function loadMatches() {
    if (currentSport === 'NBA') {
        loadNBAMatches();
    } else if (currentSport === 'Futbol') {
        loadFutbolMatches();
    } else if (currentSport === 'Tenis') {
        loadTenisMatches();
    }
}

// Cargar Partidos NBA del Día
async function loadNBAMatches() {
    const container = document.getElementById('nbaMatchesContainer');
    const dateLabel = document.getElementById('nbaMatchesDate');
    
    // Obtener fecha actual en formato YYYY-MM-DD
    const today = new Date();
    const dateString = today.toISOString().split('T')[0];
    
    // Formatear fecha para el UI
    const options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
    dateLabel.textContent = today.toLocaleDateString('es-ES', options);

    try {
        // Llama a la API (NBAMatchesController -> GET /NBAMatches?date=YYYY-MM-DD)
        const response = await fetch(`${API_BASE_URL}/NBAMatches?date=${dateString}`);
        if (!response.ok) throw new Error('Network error');
        
        const matches = await response.json();
        
        container.innerHTML = '';
        
        if (matches.length === 0) {
            container.innerHTML = '<p style="color:var(--text-muted); padding:2rem;">No hay partidos programados para este día.</p>';
            return;
        }

        matches.forEach(match => {
            const partidoMaestro = match.partidoMaestro || {};
            const estado = partidoMaestro.estado || 'Programado';
            
            const isLiveOrFinished = estado !== 'Programado';
            const statusClass = estado === 'En Curso' ? 'live' : '';
            
            // Format time to Chilean Time (CLT/CLST)
            let timeStr = '';
            if (partidoMaestro.fecha_Evento) {
                const eventDate = new Date(partidoMaestro.fecha_Evento);
                timeStr = eventDate.toLocaleTimeString('es-CL', { timeZone: 'America/Santiago', hour: '2-digit', minute: '2-digit', hour12: false });
            }

            const card = document.createElement('div');
            card.className = `match-card`;
            
            card.innerHTML = `
                <div class="match-header">
                    <span style="color:var(--text-muted);">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:inline-block; vertical-align:middle; margin-right:4px;"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                        ${timeStr}
                        <span style="margin-left:8px; font-size:0.75rem;">
                            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:inline-block; vertical-align:middle; margin-right:2px;"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>
                            ${match.estadio || 'Estadio por confirmar'}
                        </span>
                    </span>
                    <span class="match-status ${statusClass}">${estado}</span>
                </div>
                
                <div style="margin-top: 1rem;">
                    <!-- Visitante -->
                    <div class="match-team">
                        <span>${match.equipoVisitante}</span>
                        <span class="match-score" style="color: ${match.puntosVisitante > match.puntosLocal ? 'var(--success)' : 'var(--text-main)'}">${isLiveOrFinished && match.puntosVisitante !== null ? match.puntosVisitante : '-'}</span>
                    </div>
                    <!-- Local -->
                    <div class="match-team">
                        <span>${match.equipoLocal}</span>
                        <span class="match-score" style="color: ${match.puntosLocal > match.puntosVisitante ? 'var(--success)' : 'var(--text-main)'}">${isLiveOrFinished && match.puntosLocal !== null ? match.puntosLocal : '-'}</span>
                    </div>
                </div>

                <div class="match-odds">
                    <span>Cuota Local: <strong style="color:var(--accent-secondary);">${match.cuotaLocal ? match.cuotaLocal.toFixed(2) : '-'}</strong></span>
                    <span>Cuota Visita: <strong style="color:var(--accent-secondary);">${match.cuotaVisitante ? match.cuotaVisitante.toFixed(2) : '-'}</strong></span>
                </div>
            `;
            container.appendChild(card);
        });

    } catch (error) {
        container.innerHTML = '<p style="color:var(--danger); padding:2rem;">Error al cargar los partidos de la API.</p>';
        dateLabel.textContent = 'Error de Conexión';
    }
}

/* ==========================================================================
   [8] Sección: Tabla de Posiciones Oficial (Solo NBA por ahora)
   ========================================================================== */
async function loadStandings() {
    if (currentSport !== 'NBA') {
        document.getElementById('standings-east-body').innerHTML = `<tr><td colspan="9" style="padding:2rem;">Tablas de liga disponibles para NBA. En otros deportes se muestra el ELO Ranking global arriba.</td></tr>`;
        document.getElementById('standings-west-body').innerHTML = `<tr><td colspan="9">...</td></tr>`;
        return;
    }
    try {
        // Llama a la API (TeamsController -> GET /teams/standings)
        // Retorna las posiciones oficiales divididas en Conferencias Este y Oeste
        const response = await fetch(`${API_BASE_URL}/teams/standings`);
        if (!response.ok) throw new Error('Error cargando la tabla de posiciones');
        const data = await response.json();
        renderStandingsTable('standings-east-body', data.este);
        renderStandingsTable('standings-west-body', data.oeste);
    } catch (error) {
        console.error('Error fetching standings:', error);
    }
}

function renderStandingsTable(tbodyId, teamsArray) {
    const tbody = document.getElementById(tbodyId);
    if (!tbody) return;
    
    tbody.innerHTML = ''; // Limpiar tabla
    
    if (!teamsArray || teamsArray.length === 0) {
        tbody.innerHTML = `<tr><td colspan="9">No hay datos suficientes registrados en el sistema.</td></tr>`;
        return;
    }

    teamsArray.forEach(team => {
        const tr = document.createElement('tr');
        
        // Colores según posición
        const isPlayoff = team.posicion <= 6;
        const isPlayIn = team.posicion >= 7 && team.posicion <= 10;
        
        let positionBadge = `<span class="pos-badge">${team.posicion}</span>`;
        if (isPlayoff) {
            positionBadge = `<span class="pos-badge" style="background:var(--success-glow); color:var(--success); border:1px solid var(--success);">${team.posicion}</span>`;
        } else if (isPlayIn) {
            positionBadge = `<span class="pos-badge" style="background:rgba(255, 152, 0, 0.1); color:var(--warning); border:1px solid var(--warning);">${team.posicion}</span>`;
        }

        // Determinar formato positivo o negativo de la Diferencia
        const difValue = team.dif > 0 ? `+${team.dif}` : team.dif;
        const difColor = team.dif >= 0 ? 'var(--success)' : 'var(--accent-primary)';

        tr.innerHTML = `
            <td>${positionBadge}</td>
            <td style="font-weight: 600; color: var(--text-main); text-align: left;">${team.equipo}</td>
            <td>${team.pj}</td>
            <td style="color: var(--success);">${team.v}</td>
            <td style="color: var(--accent-primary);">${team.d}</td>
            <td style="font-weight: 700; color: var(--accent-secondary);">${team.pct.toFixed(3)}</td>
            <td class="hide-mobile" style="color: var(--text-muted);">${team.pf}</td>
            <td class="hide-mobile" style="color: var(--text-muted);">${team.pc}</td>
            <td style="font-weight: bold; color: ${difColor};">${difValue}</td>
        `;
        
        tbody.appendChild(tr);
    });
}

function showError(message) {
    const errorDiv = document.createElement('div');
    errorDiv.style.background = 'var(--accent-primary)';
    errorDiv.style.color = 'white';
    errorDiv.style.padding = '10px';
    errorDiv.style.borderRadius = '5px';
    errorDiv.style.marginBottom = '10px';
    errorDiv.textContent = message;
    document.querySelector('.dashboard-container').prepend(errorDiv);
    
    setTimeout(() => errorDiv.remove(), 5000);
}

// Carga las ligas disponibles y puebla el selector
async function loadFutbolLigas() {
    try {
        const response = await fetch(`${API_BASE_URL}/Futbol/ligas`);
        const ligas = await response.json();
        const selector = document.getElementById('ligaSelector');
        if (!selector) return;
        
        // Conservar la opción "Todas las Ligas" y agregar las disponibles
        selector.innerHTML = '<option value="">⚽ Todas las Ligas</option>';
        ligas.forEach(liga => {
            const opt = document.createElement('option');
            opt.value = liga;
            opt.textContent = liga;
            selector.appendChild(opt);
        });
    } catch (e) {
        console.warn('No se pudieron cargar las ligas:', e);
    }
}

// Carga partidos de Fútbol (con filtro opcional por liga)
async function loadFutbolMatches() {
    const container = document.getElementById('nbaMatchesContainer');
    container.innerHTML = '<div class="spinner"></div>';
    try {
        // Leer liga seleccionada del selector
        const ligaSelector = document.getElementById('ligaSelector');
        const ligaFiltro = ligaSelector ? ligaSelector.value : '';
        const url = ligaFiltro
            ? `${API_BASE_URL}/Futbol/matches?liga=${encodeURIComponent(ligaFiltro)}`
            : `${API_BASE_URL}/Futbol/matches`;
        
        const response = await fetch(url);
        const matches = await response.json();
        container.innerHTML = '';
        
        if (!Array.isArray(matches)) {
            container.innerHTML = '<p style="color:var(--text-muted); padding:2rem;">No hay partidos de fútbol disponibles.</p>';
            return;
        }

        if (matches.length === 0) {
            container.innerHTML = '<p style="color:var(--text-muted); padding:2rem;">No hay partidos de fútbol registrados.</p>';
            return;
        }
        matches.forEach(m => {
            const card = document.createElement('div');
            card.className = 'match-card';
            const prob = m.probabilidades || { probLocal: 0.33, probEmpate: 0.34, probVisita: 0.33 };
            
            // Formatear fecha y hora desde el PartidoMaestro o campo directo
            const fechaRaw = m.fechaEvento || m.partidoMaestro?.fecha_Evento || m.partidoMaestro?.fechaEvento;
            let fechaStr = 'Fecha por confirmar';
            let horaStr = '';
            if (fechaRaw) {
                const d = new Date(fechaRaw);
                fechaStr = d.toLocaleDateString('es-CL', { weekday: 'short', day: 'numeric', month: 'short' });
                horaStr = d.toLocaleTimeString('es-CL', { hour: '2-digit', minute: '2-digit' });
            }
            const estadio = m.estadio || m.liga || 'Estadio por confirmar';
            
            card.innerHTML = `
                <div class="match-header">
                    <span>⚽ Fútbol</span>
                    <span class="match-status">${m.estado || 'Programado'}</span>
                </div>
                <div class="match-date-info" style="font-size:0.78rem; color:var(--text-muted); margin-bottom:0.6rem; display:flex; gap:10px; flex-wrap:wrap;">
                    <span>📅 ${fechaStr}${horaStr ? ' · ' + horaStr : ''}</span>
                    <span>📍 ${estadio}</span>
                </div>
                <div style="margin: 0.8rem 0;">
                    <div class="match-team"><span>${m.equipoLocal}</span></div>
                    <div class="match-team"><span>${m.equipoVisitante}</span></div>
                </div>
                <div class="match-odds" style="font-size: 0.8rem; display:flex; gap:10px;">
                    <span>L: <strong>${(prob.probLocal * 100).toFixed(1)}%</strong></span>
                    <span>X: <strong>${(prob.probEmpate * 100).toFixed(1)}%</strong></span>
                    <span>V: <strong>${(prob.probVisita * 100).toFixed(1)}%</strong></span>
                </div>
            `;
            container.appendChild(card);
        });
    } catch (e) { 
        console.error(e);
        container.innerHTML = '<p style="color:var(--danger); padding:2rem;">Error al cargar fútbol.</p>';
    }
}

// Carga torneos y partidos de Tenis
async function loadTenisMatches() {
    const container = document.getElementById('nbaMatchesContainer');
    container.innerHTML = '<div class="spinner"></div>';
    try {
        // Llama a la API (TenisController -> GET /Tenis/matches)
        const response = await fetch(`${API_BASE_URL}/Tenis/matches`);
        const matches = await response.json();
        container.innerHTML = '';

        if (!Array.isArray(matches)) {
            container.innerHTML = '<p style="color:var(--text-muted); padding:2rem;">No hay partidos de tenis disponibles.</p>';
            return;
        }

        if (matches.length === 0) {
            container.innerHTML = '<p style="color:var(--text-muted); padding:2rem;">No hay partidos de tenis registrados.</p>';
            return;
        }
        matches.forEach(m => {
            const card = document.createElement('div');
            card.className = 'match-card tennis-match';
            
            // Formatear fecha y hora
            const fechaRaw = m.fechaEvento || m.partidoMaestro?.fecha_Evento || m.partidoMaestro?.fechaEvento;
            let fechaStr = 'Fecha por confirmar';
            let horaStr = '';
            if (fechaRaw) {
                const d = new Date(fechaRaw);
                fechaStr = d.toLocaleDateString('es-CL', { weekday: 'short', day: 'numeric', month: 'short' });
                horaStr = d.toLocaleTimeString('es-CL', { hour: '2-digit', minute: '2-digit' });
            }
            const sede = m.torneo || 'Sede por confirmar';
            const superficie = m.superficie || 'Hard';
            const probJ1 = (m.probabilidadJ1 * 100).toFixed(1);
            const probJ2 = ((1 - m.probabilidadJ1) * 100).toFixed(1);

            card.innerHTML = `
                <div class="match-header">
                    <span>🎾 ${m.torneo || 'Tenis'}</span>
                    <span class="match-status">${m.estado || 'Programado'}</span>
                </div>
                <div class="match-date-info" style="font-size:0.78rem; color:var(--text-muted); margin-bottom:0.6rem; display:flex; gap:10px; flex-wrap:wrap;">
                    <span>📅 ${fechaStr}</span>
                    <span>📍 ${superficie}</span>
                </div>
                <div style="margin: 0.8rem 0; display:flex; flex-direction:column; gap:8px;">
                    <div class="match-team" style="display:flex; justify-content:space-between; align-items:center;">
                        <span>${m.jugador1}</span>
                        <span class="prob-badge" style="background:var(--accent-glow); color:var(--accent-primary); padding:2px 6px; border-radius:4px; font-size:0.8rem; font-weight:bold;">${probJ1}%</span>
                    </div>
                    <div class="match-team" style="display:flex; justify-content:space-between; align-items:center;">
                        <span>${m.jugador2}</span>
                        <span class="prob-badge" style="background:rgba(255,255,255,0.05); color:var(--text-muted); padding:2px 6px; border-radius:4px; font-size:0.8rem; font-weight:bold;">${probJ2}%</span>
                    </div>
                </div>
            `;
            container.appendChild(card);
        });
    } catch (e) {
        console.error(e);
        container.innerHTML = '<p style="color:var(--danger); padding:2rem;">Error al cargar tenis.</p>';
    }
}

function makePanelsCollapsible() {
    const headers = document.querySelectorAll('.panel-header, .section-header');
    headers.forEach(header => {
        header.style.cursor = 'pointer';
        header.title = 'Haz clic para expandir o contraer';

        // Re-añadir el icono si se borró por un updateUIForSport
        if (!header.querySelector('.toggle-icon')) {
            const titleEl = header.querySelector('h2');
            if (titleEl) {
                const icon = document.createElement('span');
                icon.className = 'toggle-icon';
                icon.innerHTML = `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-left: 8px; vertical-align: middle;"><polyline points="6 9 12 15 18 9"></polyline></svg>`;
                titleEl.appendChild(icon);
            }
        }

        // Evitar múltiples listeners en caso de re-renderizados
        if (header.dataset.collapsibleAttached) return;
        header.dataset.collapsibleAttached = 'true';

        header.addEventListener('click', (e) => {
            // No colapsar si se hace click en controles interactivos internos
            if (e.target.tagName.toLowerCase() === 'input' || e.target.tagName.toLowerCase() === 'select') {
                return;
            }
            const parent = header.closest('.panel') || header.closest('.standings-section');
            if (parent) {
                parent.classList.toggle('collapsed');
            }
        });
    });
}

