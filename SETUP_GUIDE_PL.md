# 📖 Instrukcja Konfiguracji Systemu Flying AI Swarm

Ten plik zawiera szczegółowy przewodnik krok po kroku, który pozwoli Ci szybko skonfigurować i przetestować system Flying AI Swarm w pustej scenie Unity.

---

## 🛠️ Krok 1: Wymagane Pakiety (Dependencies)
Przed rozpoczęciem upewnij się, że w **Window** > **Package Manager** masz zainstalowane następujące pakiety:
1. **Burst** (`com.unity.burst`)
2. **Mathematics** (`com.unity.mathematics`)
3. **Input System** (`com.unity.inputsystem`)

---

## 📂 Krok 2: Utworzenie Folderu Resources i Ustawień Loggera
System używa optymalizowanego loggera, który wymaga pliku konfiguracyjnego w folderze `Resources`:
1. W oknie Project kliknij prawym przyciskiem myszy na folder **Assets** > **Create** > **Folder** i nazwij go `Resources`.
2. Kliknij prawym przyciskiem myszy wewnątrz folderu `Resources` > **Create** > **FlyingAI** > **Settings** > **Debug Logger**.
3. Nazwij utworzony plik dokładnie: `DebugLoggerSettings`.
4. W inspektorze upewnij się, że:
   * ✅ **Enable Logging** jest zaznaczone.
   * **Log Level** jest ustawiony na `Info`.
   * Filtry poszczególnych systemów są włączone.

---

## ⚙️ Krok 3: Utworzenie Konfiguracji Wroga i Formacji
System wykorzystuje ScriptableObjects do łatwej parametryzacji wrogów oraz formacji:
1. Kliknij prawym przyciskiem myszy w oknie Project > **Create** > **FlyingAI** > **Configs** > **Flying Enemy Config**.
   * Nazwij go: `BasicEnemyConfig`.
   * Ustaw wartości np. *Max Health* = `100`, *Move Speed* = `12`, *Base Priority* = `5`.
2. Kliknij prawym przyciskiem myszy w oknie Project > **Create** > **FlyingAI** > **Configs** > **Formation Config**.
   * Nazwij go: `VFormationConfig`.
   * Wybierz *Type* = `V`.
   * Ustaw *Max Slots* = `15`, *Horizontal Spacing* = `3`, *V Angle* = `45`.

---

## 🛸 Krok 4: Konfiguracja Prefabu Wroga (Enemy Prefab)
1. Stwórz pusty obiekt w hierarchii sceny: **Right-click** w Hierarchy > **Create Empty**. Nazwij go `FlyingEnemy`.
2. Dodaj do niego następujące komponenty:
   * **FlyingEnemy** (główny skrypt wroga)
   * **SphereCollider** – zaznacz opcję **Is Trigger** = ✅, *Radius* = `1.2`.
   * **Rigidbody** – odznacz opcję **Use Gravity** = ❌, zaznacz **Is Kinematic** = ✅.
3. Jako podrzędny obiekt (child) dodaj jakiś model trójwymiarowy (np. **Right-click** na `FlyingEnemy` > **3D Object** > **Sphere**). **Ważne:** Usuń collider z obiektu podrzędnego, aby nie zakłócał fizyki.
4. W komponencie `FlyingEnemy` na obiekcie nadrzędnym przypisz utworzony wcześniej `BasicEnemyConfig` do pola **Enemy Config**.
5. Przeciągnij obiekt `FlyingEnemy` z hierarchii do folderu projektu (np. `Assets/Prefabs/`), aby zapisać go jako Prefab.
6. Usuń obiekt `FlyingEnemy` z hierarchii sceny.

---

## 🏛️ Krok 5: Konfiguracja Sceny Testowej
Utwórz strukturę obiektów zarządzających swarmem w scenie:

1. **Stwórz GameManager**:
   * Stwórz pusty obiekt i nazwij go `GameManager`.
   * Dodaj do niego następujące skrypy (zarządcy systemu):
     * **SwarmController**
     * **FormationManager**
     * **SpawnManager**
     * **EnemyPrioritySystem**
     * **FlockingSystem**

2. **Stwórz EnemyPool (Pula Obiektów)**:
   * Stwórz pusty obiekt i nazwij go `EnemyPool`.
   * Dodaj komponent **EnemyPool**.
   * Przypisz swój prefab `FlyingEnemy` do pola **Enemy Prefab**.
   * Ustaw *Initial Pool Size* = `30`, *Max Pool Size* = `150`.

3. **Połącz komponenty w GameManager**:
   * W **SpawnManager**: przypisz obiekt `EnemyPool` do pola *Enemy Pool*.
   * Stwórz kilka pustych obiektów w scenie i nazwij je np. `SpawnPoint_1`, `SpawnPoint_2`. Przypisz je do listy *Spawn Points* w **SpawnManager**.

4. **Stwórz Cel dla Swarmu (Player Target)**:
   * Stwórz kostkę lub inny obiekt w scenie, nazwij go `PlayerTarget`.
   * W komponencie **SwarmController** w GameManagerze przypisz obiekt `PlayerTarget` do pola **Primary Target**.

5. **Skonfiguruj DemoSpawner**:
   * Stwórz pusty obiekt o nazwie `DemoSpawner` i dodaj komponent **DemoSpawner**.
   * Przypisz:
     * *Enemy Config* = `BasicEnemyConfig`
     * *Formation Config* = `VFormationConfig`
     * *Spawn Count* = `10`
     * *Spawn Offset* = `(0, 0, 0)`

---

## 📐 Podsumowanie Hierarchii Sceny
Twoja scena powinna wyglądać następująco:
```
📂 NazwaSceny
├── 📦 GameManager (SwarmController, FormationManager, SpawnManager, EnemyPrioritySystem, FlockingSystem)
├── 📦 EnemyPool (Skrypt EnemyPool z przypisanym prefabem wroga)
├── 📦 SpawnPoints
│   ├── SpawnPoint_1 (Pozycja np. -20, 10, 0)
│   └── SpawnPoint_2 (Pozycja np. 20, 10, 0)
├── 📦 PlayerTarget (Kostka/Gracz - cel dla wrogów)
└── 📦 DemoSpawner (Skrypt sterujący testami)
```

---

## 🎮 Krok 6: Testowanie i Skróty Klawiszowe
1. Uruchom tryb gry (**Play Mode**).
2. Użyj następujących skrótów klawiszowych (obsługiwanych przez nowy Input System za pośrednictwem klasy `Keyboard`):

| Klawisz | Akcja |
| :--- | :--- |
| **`S`** | Spawnuje jednego wroga w losowym punkcie |
| **`F`** | Spawnuje pełną eskadrę (10 wrogów) w formacji wokół punktu spawnu |
| **`K`** | Zabija wszystkich aktywnych wrogów (zwraca ich do puli `EnemyPool`) |

3. Otwórz dedykowany **Log Manager**:
   * Przejdź do: **Window** > **FlyingAI** > **Log Manager** (lub użyj skrótu **`Ctrl+Shift+L`**).
   * Okno pozwala na monitoring działania systemu w czasie rzeczywistym, filtrowanie logów według kategorii oraz kopiowanie całości lub samych błędów do schowka za pomocą jednego kliknięcia.

---

## 🛠️ Rozwiązywanie problemów (Troubleshooting)

* **Błąd: *InvalidOperationException* dot. Input System**:
  * Zaktualizowaliśmy `DemoSpawner.cs` tak, aby automatycznie wykrywał aktywny system sterowania. Jeśli nadal występuje błąd, upewnij się, że w *Player Settings* > *Active Input Handling* masz wybraną opcję **Both** lub **Input System Package (New)**.
* **Wrogowie się nie poruszają**:
  * Upewnij się, że w `SwarmController` jest przypisany cel (*Primary Target*) oraz że komponent `FlockingSystem` jest aktywny w scenie.
  * Sprawdź, czy wrogowie mają prawidłowo przypisany `FlyingEnemyConfig`.
* **Brak logów w konsoli**:
  * Sprawdź, czy plik `DebugLoggerSettings` znajduje się dokładnie w folderze o nazwie `Resources` (musi być to główny folder Resources, np. `Assets/Resources/DebugLoggerSettings`).
