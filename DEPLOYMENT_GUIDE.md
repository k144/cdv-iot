# Przewodnik Wdrożeniowy: Parking Spot Finder

Ten przewodnik zawiera instrukcje dotyczące wdrażania aplikacji Parking Spot Finder na platformie Microsoft Azure przy użyciu dostarczonych skryptów powłoki.

## Wymagania Wstępne

- [Azure CLI](https://docs.microsoft.com/pl-pl/cli/azure/install-azure-cli)
- [Docker](https://docs.docker.com/get-docker/)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/pl-pl/azure/azure-functions/functions-run-local)

## Zarządzanie Konfiguracją

Przed wdrożeniem zapoznaj się z plikiem `SECRETS.md`, aby dowiedzieć się, jak zarządzać wrażliwymi danymi konfiguracyjnymi (sekretami) zarówno w środowisku lokalnym, jak i w Azure. Aplikacja jest skonfigurowana do odczytywania sekretów z `User Secrets` lokalnie i z `Application Settings` w Azure.

## Kroki Wdrożenia

Proces wdrożenia jest podzielony na dwa główne skrypty:

1.  `setup-azure-resources.sh`: Tworzy niezbędną infrastrukturę na platformie Azure.
2.  `deploy-services.sh`: Buduje, pakuje i wdraża usługi aplikacji.

### Krok 1: Konfiguracja Zasobów Azure

Ten skrypt tworzy podstawowe zasoby Azure wymagane przez aplikację.

1.  **Zaloguj się do Azure:**
    ```bash
    az login
    ```

2.  **Uruchom skrypt konfiguracyjny:**
    ```bash
    ./setup-azure-resources.sh
    ```

Skrypt wygeneruje plik `azure-config.env` zawierający nazwy utworzonych zasobów. Ten plik jest używany przez skrypt wdrożeniowy w następnym kroku.

### Krok 2: Wdrożenie Usług Aplikacji

Ten skrypt wdraża mikrousługi aplikacji na zasobach Azure utworzonych w poprzednim kroku.

1.  **Utwórz plik `deployment-config.env`:**

    Utwórz plik o nazwie `deployment-config.env` i dodaj następującą zawartość, zastępując wartości zastępcze swoimi sekretami. **Pamiętaj, aby nie commitować tego pliku do repozytorium Git!**

    ```env
    # Ciąg połączenia PostgreSQL
    POSTGRES_CONNECTION_STRING="your-postgres-connection-string-here"

    # Identyfikator subskrypcji Azure
    AZURE_SUBSCRIPTION_ID="your-subscription-id-here"

    # Klucz JWT (wygeneruj silny, losowy klucz)
    JWT_KEY="your-strong-jwt-key-here"
    ```

2.  **Uruchom skrypt wdrożeniowy:**
    ```bash
    ./deploy-services.sh
    ```

Ten skrypt wykonuje następujące czynności:
- Loguje się do Azure Container Registry.
- Buduje obrazy Docker dla usług `RestApi`, `Camera` i `AiVisionModel`.
- Wypycha obrazy Docker do Azure Container Registry.
- Wdraża `RestApi` do Azure App Service, konfigurując sekrety z pliku `deployment-config.env` jako ustawienia aplikacji.
- Wdraża `AiVisionModel` do Azure Container Instance.
- Wdraża funkcje Azure `ImageDownloader` i `ImageProcessor`.

Po pomyślnym zakończeniu skrypt utworzy plik `service-urls.env` zawierający publiczne adresy URL wdrożonych usług.

## Po Wdrożeniu

Po uruchomieniu obu skryptów system Parking Spot Finder zostanie w pełni wdrożony i będzie działał na platformie Azure. Możesz użyć adresów URL z pliku `service-urls.env`, aby wejść w interakcję z wdrożoną aplikacją.

Aby przetestować wdrożone usługi, możesz uruchomić skrypt `test-deployment.sh`:

```bash
./test-deployment.sh
```