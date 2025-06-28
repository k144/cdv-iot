# Przewodnik Testowania: Parking Spot Finder

Ten przewodnik zawiera instrukcje dotyczące testowania systemu Parking Spot Finder, w tym nowych funkcji uwierzytelniania i zarządzania użytkownikami.

## Wymagania Wstępne

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [curl](https://curl.se/)
- [jq](https://stedolan.github.io/jq/)

## Konfiguracja Lokalna

Przed uruchomieniem testów upewnij się, że skonfigurowałeś swoje lokalne sekrety (User Secrets) dla projektu `RestApi`. Szczegółowe instrukcje znajdują się w pliku `SECRETS.md`.

## Szybki Start: Testowanie Zautomatyzowane

Skrypt `run-tests.sh` sprawdza zależności, buduje wszystkie projekty, a następnie wykonuje skrypt `test-system.sh` w celu przeprowadzenia pełnego testu integracyjnego.

```bash
./run-tests.sh
```

## Ręczne Kroki Testowania

### 1. Migracja Bazy Danych

Przed pierwszym uruchomieniem aplikacji należy zastosować migracje bazy danych, aby utworzyć niezbędne tabele, w tym `Users`.

1.  **Zainstaluj `dotnet-ef` (jeśli jeszcze tego nie zrobiłeś):**
    ```bash
    dotnet tool install --global dotnet-ef
    ```

2.  **Zastosuj migrację:**
    ```bash
    dotnet ef database update --project ParkingSpotFinder/Database
    ```

### 2. Uruchom Usługi

Otwórz osobne terminale dla każdej z poniższych usług i uruchom je:

**Terminal 1 - REST API:**
```bash
cd ParkingSpotFinder/RestApi
dotnet run --urls http://localhost:5000
```

**Terminal 2 - Usługa Kamery:**
```bash
cd ParkingSpotFinder/Camera
dotnet run --urls http://localhost:5001
```

**Terminal 3 - Model AI Vision:**
```bash
cd ParkingSpotFinder/AiVisionModel
dotnet run --urls http://localhost:5002
```

### 3. Uwierzytelnianie i Zarządzanie Użytkownikami

#### Zarejestruj Nowego Użytkownika

```bash
curl -X POST "http://localhost:5000/api/Users/register?username=testuser&password=password123" -v
```

#### Zaloguj się i Uzyskaj Token JWT

```bash
TOKEN=$(curl -s -X POST "http://localhost:5000/api/Users/login?username=testuser&password=password123")
echo "JWT: $TOKEN"
```

### 4. Przetestuj Zabezpieczone Punkty Końcowe

Teraz, gdy masz token JWT, możesz go użyć do uzyskania dostępu do zabezpieczonych punktów końcowych.

#### Pobierz Wszystkie Parkingi (z uwierzytelnianiem)

```bash
curl -X GET "http://localhost:5000/api/ParkingLots" -H "Authorization: Bearer $TOKEN"
```

#### Utwórz Nowy Parking (z uwierzytelniem)

```bash
cURL -X POST "http://localhost:5000/api/ParkingLots" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "Authenticated Test Lot",
    "location": "789 Secure Ave",
    "totalParkingSpaces": 75
  }'
```

## Scenariusze Testowe

### Scenariusz 1: Nieautoryzowany Dostęp

Spróbuj uzyskać dostęp do zabezpieczonego punktu końcowego bez tokena JWT.

```bash
curl -X GET "http://localhost:5000/api/ParkingLots" -v
```

Powinieneś otrzymać odpowiedź `401 Unauthorized`.

### Scenariusz 2: Nieprawidłowy Token

Spróbuj uzyskać dostęp do zabezpieczonego punktu końcowego przy użyciu nieprawidłowego lub wygasłego tokena JWT.

```bash
curl -X GET "http://localhost:5000/api/ParkingLots" -H "Authorization: Bearer invalidtoken" -v
```

Powinieneś również otrzymać odpowiedź `401 Unauthorized`.

## Kryteria Sukcesu

System działa poprawnie, jeśli:

- Użytkownicy mogą pomyślnie rejestrować się i logować.
- Punkt końcowy logowania zwraca prawidłowy token JWT.
- Zabezpieczone punkty końcowe zwracają błąd `401 Unauthorized` w przypadku dostępu bez prawidłowego tokena JWT.
- Zabezpieczone punkty końcowe zwracają oczekiwane dane w przypadku dostępu z prawidłowym tokenem JWT.
- Wszystkie wcześniej istniejące funkcje działają zgodnie z oczekiwaniami.
