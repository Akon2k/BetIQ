import requests
import json
from datetime import datetime

# Configuracion
API_KEY = 'ce14ed2f59775e2493cf2307f1231c9d'
REGIONS = 'eu,us'
MARKETS = 'h2h'
ODDS_FORMAT = 'decimal'

SPORTS_CONFIG = {
    'basketball_nba': 'http://localhost:5023/api/NBAMatches/odds/batch',
    'soccer_epl': 'http://localhost:5023/api/Futbol/odds/batch',
    'tennis_atp_aus_open': 'http://localhost:5023/api/Tenis/odds/batch'
}

def get_odds(sport_key):
    print(f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] Obteniendo cuotas para {sport_key}...")
    url = f"https://api.the-odds-api.com/v4/sports/{sport_key}/odds"
    params = {'apiKey': API_KEY, 'regions': REGIONS, 'markets': MARKETS, 'oddsFormat': ODDS_FORMAT}
    try:
        response = requests.get(url, params=params)
        response.raise_for_status()
        return response.json()
    except Exception as e:
        print(f"Error {sport_key}: {e}")
        return None

def sync_sport(sport_key, api_url):
    data = get_odds(sport_key)
    if not data: return

    batch = []
    for game in data:
        home_team = game['home_team']
        away_team = game['away_team']
        
        # Promediar cuotas
        h_odds, v_odds, x_odds = [], [], []
        for bk in game.get('bookmakers', []):
            for mkt in bk.get('markets', []):
                if mkt['key'] == 'h2h':
                    for out in mkt.get('outcomes', []):
                        if out['name'] == home_team: h_odds.append(out['price'])
                        elif out['name'] == away_team: v_odds.append(out['price'])
                        elif out['name'].lower() == 'draw': x_odds.append(out['price'])

        if not h_odds or not v_odds: continue
        
        batch.append({
            "EquipoLocal": home_team.split(" ")[-1] if sport_key == 'basketball_nba' else home_team,
            "EquipoVisitante": away_team.split(" ")[-1] if sport_key == 'basketball_nba' else away_team,
            "FechaEventoString": game['commence_time'].split('T')[0],
            "CuotaPromedioLocal": round(sum(h_odds)/len(h_odds), 2),
            "CuotaPromedioVisita": round(sum(v_odds)/len(v_odds), 2),
            "CuotaPromedioEmpate": round(sum(x_odds)/len(x_odds), 2) if x_odds else 0
        })

    if batch:
        print(f"Enviando {len(batch)} partidos a {api_url}...")
        requests.post(api_url, json=batch)

if __name__ == "__main__":
    for sport, url in SPORTS_CONFIG.items():
        sync_sport(sport, url)
