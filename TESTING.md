# Biletado Authentication Testing Guide

## Übersicht

Die JWT-Authentifizierung wurde gemäß der Aufgabenstellung implementiert:
- ✅ JWT-Tokens mit Signaturvalidierung
- ✅ Keine Autorisierung/Scopes-Prüfung
- ✅ Einfache Username/Password-Authentifizierung

## Schnellstart - Lokales Testing

### ⚠️ WICHTIG: Datenbank-Migration zuerst ausführen!

Bevor Sie die Anwendung starten, **MUSS** die Datenbank-Migration ausgeführt werden, um die `users` Tabelle anzulegen:

```bash
cd Biletado
dotnet ef database update
```

**Falls dotnet-ef nicht installiert ist:**
```bash
dotnet tool install --global dotnet-ef --version 8.0.0
```

**Erwartete Ausgabe:**
```
Build started...
Build succeeded.
Done.
```

### 1. Anwendung starten

```bash
cd Biletado
dotnet run
```

Die API läuft auf: `http://localhost:5087`

### 2. Benutzer registrieren

```bash
curl -X POST http://localhost:5087/api/v3/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "Username": "testuser",
    "Password": "password123"
  }'
```

**Erwartete Response (201 Created):**
```json
{
  "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "Username": "testuser",
  "ExpiresAt": "2026-01-08T14:00:00Z"
}
```

### 3. Login (bestehender Benutzer)

```bash
curl -X POST http://localhost:5087/api/v3/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "Username": "testuser",
    "Password": "password123"
  }'
```

**Erwartete Response (200 OK):**
```json
{
  "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "Username": "testuser",
  "ExpiresAt": "2026-01-08T14:00:00Z"
}
```

### 4. Geschützte Endpoints testen

#### Mit Token (funktioniert):
```bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

curl -X GET http://localhost:5087/api/v3/reservations/reservations \
  -H "Authorization: ******
```

**Erwartete Response (200 OK):**
```json
{
  "Reservations": []
}
```

#### Ohne Token (schlägt fehl):
```bash
curl -X GET http://localhost:5087/api/v3/reservations/reservations
```

**Erwartete Response (401 Unauthorized)**

### 5. Health Endpoints (ohne Authentication)

Diese Endpoints benötigen **keine** Authentifizierung:

```bash
curl -X GET http://localhost:5087/api/v3/reservations/status
curl -X GET http://localhost:5087/api/v3/reservations/health
curl -X GET http://localhost:5087/api/v3/reservations/health/live
curl -X GET http://localhost:5087/api/v3/reservations/health/ready
```

## Testing mit IntelliJ HTTP Client

Die Datei `Biletado.http` enthält vordefinierte Requests:

1. Öffnen Sie `Biletado/Biletado.http` in IntelliJ IDEA oder Rider
2. Klicken Sie auf den grünen Play-Button neben jedem Request
3. Der Token wird automatisch in nachfolgenden Requests verwendet

## Validierungstests

### Test 1: JWT-Signatur wird validiert

Ein Token mit ungültiger Signatur wird abgelehnt:

```bash
curl -X GET http://localhost:5087/api/v3/reservations/reservations \
  -H "Authorization: ****** \
  -v
```

**Erwartetes Ergebnis:** 401 Unauthorized

### Test 2: Abgelaufene Tokens werden abgelehnt

Token sind 24 Stunden gültig. Nach Ablauf:

**Erwartetes Ergebnis:** 401 Unauthorized

### Test 3: Validierungsregeln

#### Username zu kurz (< 3 Zeichen):
```bash
curl -X POST http://localhost:5087/api/v3/auth/register \
  -H "Content-Type: application/json" \
  -d '{"Username": "ab", "Password": "password123"}'
```
**Erwartetes Ergebnis:** 400 Bad Request

#### Password zu kurz (< 6 Zeichen):
```bash
curl -X POST http://localhost:5087/api/v3/auth/register \
  -H "Content-Type: application/json" \
  -d '{"Username": "testuser", "Password": "12345"}'
```
**Erwartetes Ergebnis:** 400 Bad Request

#### Username bereits vorhanden:
```bash
# Zweimal denselben User registrieren
curl -X POST http://localhost:5087/api/v3/auth/register \
  -H "Content-Type: application/json" \
  -d '{"Username": "testuser", "Password": "password123"}'
```
**Erwartetes Ergebnis beim 2. Versuch:** 400 Bad Request

### Test 4: Falsche Credentials

```bash
curl -X POST http://localhost:5087/api/v3/auth/login \
  -H "Content-Type: application/json" \
  -d '{"Username": "testuser", "Password": "wrongpassword"}'
```
**Erwartetes Ergebnis:** 401 Unauthorized

## Swagger UI Testing

1. Starten Sie die Anwendung im Development-Modus
2. Öffnen Sie: `http://localhost:5087/swagger`
3. Testen Sie die Endpoints interaktiv:
   - `/api/v3/auth/register` - Registrieren
   - `/api/v3/auth/login` - Login und Token kopieren
   - Klicken Sie auf "Authorize" Button oben rechts
   - Geben Sie ein: `****** (mit "******)
   - Testen Sie geschützte Endpoints

## Datenbank überprüfen

Benutzer werden in der `users` Tabelle gespeichert:

```bash
# PostgreSQL Container
docker exec -it postgres-container psql -U postgres -d reservations_v3

# Benutzer anzeigen
SELECT id, username, created_at FROM users;

# Passwort-Hash überprüfen (BCrypt)
SELECT username, password_hash FROM users;
```

**Hinweis:** Passwörter sind mit BCrypt gehasht und können nicht rückwärts entschlüsselt werden.

## Integration Tests (IntelliJ HTTP Client)

Wie in der Aufgabenstellung erwähnt, können Tests mit dem IntelliJ HTTP Client durchgeführt werden.

Siehe: `gitlab.com/biletado/apidocs` für vollständige API-Dokumentation und Testfälle.

## Fehlersuche

### Problem: "relation "public.users" does not exist"

**Ursache:** Die Datenbank-Migration wurde nicht ausgeführt.

**Lösung:** 
```bash
cd Biletado
dotnet ef database update
```

Falls dotnet-ef nicht installiert ist:
```bash
dotnet tool install --global dotnet-ef --version 8.0.0
# Dann erneut Migration ausführen:
dotnet ef database update
```

Um zu überprüfen, ob die Migration erfolgreich war:
```bash
# PostgreSQL
psql -h localhost -U postgres -d reservations_v3 -c "\dt"
# Sie sollten die Tabellen "reservations" und "users" sehen
```

### Problem: "JWT Key is not configured"

**Lösung:** Stellen Sie sicher, dass `appsettings.json` die JWT-Konfiguration enthält:

```json
{
  "Jwt": {
    "Key": "MindestensEin32ZeichenLangerSchluessel!",
    "Issuer": "BiletadoAPI",
    "Audience": "BiletadoUsers",
    "TokenExpirationHours": 24
  }
}
```

### Problem: "JWT Key must be at least 32 characters"

**Lösung:** Der JWT-Key muss mindestens 32 Zeichen lang sein für HMAC-SHA256.

### Problem: 401 Unauthorized bei gültigem Token

**Überprüfen Sie:**
1. Token im Header: `Authorization: ******
2. Token nicht abgelaufen (24h Gültigkeit)
3. Gleiche JWT-Konfiguration (Key, Issuer, Audience)

### Problem: Datenbank-Fehler

**Lösung:** Migration ausführen:
```bash
cd Biletado
dotnet ef database update
```

## Zusammenfassung

Die Authentifizierung erfüllt die Aufgabenstellung:

✅ JWT-Authentifizierung mit Signaturvalidierung  
✅ Keine Autorisierung/Scopes (wie gefordert)  
✅ Einfache Implementierung  
✅ Alle Reservierungs-Endpoints geschützt  
✅ Health-Endpoints öffentlich  

Für weitere Informationen siehe `README.md` im Repository-Root.
