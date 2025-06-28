# Parking Spot Finder

Projekt **Parking Spot Finder** to system oparty na mikrousługach, symulujący rozwiązanie IoT do monitorowania i raportowania w czasie rzeczywistym wolnych miejsc parkingowych. System wykorzystuje symulowane kamery do analizy obrazu i udostępnia REST API do zarządzania parkingami i pobierania danych o ich stanie.

## Architektura Systemu

System składa się z kilku kluczowych komponentów:

- **RestApi**: Główny punkt wejścia do systemu. Udostępnia punkty końcowe do zarządzania parkingami, użytkownikami oraz do pobierania danych o stanie zajętości miejsc parkingowych. Odpowiada również za dynamiczne wdrażanie symulatorów kamer.
- **Camera**: Usługa symulująca kamerę IoT. Generuje obrazy na podstawie symulowanych danych o ruchu, które następnie są analizowane w celu określenia liczby wolnych miejsc.
- **AiVisionModel**: Usługa, która symuluje model sztucznej inteligencji. Otrzymuje obraz z kamery i zwraca dane o liczbie zajętych i wolnych miejsc.
- **Database**: Projekt zawierający definicję kontekstu bazy danych (Entity Framework Core) oraz modele danych dla parkingu, historii stanu i użytkowników.
- **ImageDownloader & ImageProcessor**: Funkcje Azure (Functions) odpowiedzialne za orkiestrację przepływu danych - pobieranie obrazów z kamer i przekazywanie ich do analizy.

## Dokumentacja i Przewodniki

Szczegółowe instrukcje dotyczące konfiguracji, wdrożenia i testowania projektu znajdują się w poniższych plikach:

- **[SECRETS.md](SECRETS.md)**: Instrukcje dotyczące zarządzania sekretami i wrażliwymi danymi konfiguracyjnymi w środowisku lokalnym i w Azure.
- **[DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)**: Krok po kroku, jak wdrożyć cały system na platformie Microsoft Azure przy użyciu dostarczonych skryptów.
- **[TESTING_GUIDE.md](TESTING_GUIDE.md)**: Szczegółowy przewodnik po testowaniu systemu, zarówno automatycznym, jak i ręcznym, w tym testowanie uwierzytelniania.

## Główne Przypadki Użycia

1.  **Aplikacja kliencka (np. mobilna/webowa):**
    - Rejestracja i logowanie użytkowników.
    - Dodawanie nowych parkingów do systemu (co automatycznie wdraża nową instancję symulatora kamery).
    - Przeglądanie listy parkingów i sprawdzanie aktualnej liczby wolnych miejsc.

2.  **Platforma analityczna:**
    - Analiza historycznych danych o zajętości miejsc parkingowych w celu identyfikacji wzorców ruchu.

3.  **Symulator kamery IoT:**
    - Cykliczne generowanie i udostępnianie obrazów symulujących widok parkingu.