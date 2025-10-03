# 🎓 CRUIS Auto Grader

CRUIS Auto Grader je distribuirani sistem za **automatsku analizu i
ocenjivanje edukativnih radova** (eseja, projekata, koda).
Sistem koristi **mikroservisnu arhitekturu** implementiranu u
**Microsoft Service Fabric** i podržava više korisničkih rola:
**Student**, **Profesor**, i **Administrator**.

------------------------------------------------------------------------

## 🚀 Funkcionalnosti

-   📥 **Student**:
    -   Predaja radova u različitim formatima (`.txt`, `.pdf`, `.docx`,
        `.zip`, `.py`, `.cs`, itd.)
    -   Višestruke verzije radova (student može pregledati i vratiti se
        na starije verzije)
    -   Pregled istorije sopstvenih radova i ocjena
    -   Pregled povratnih informacija i predloga za poboljšanje
    -   Analiza napretka tokom vremena (grafički prikaz u dashboard-u)
-   👨‍🏫 **Profesor**:
    -   Pregled svih radova svojih studenata
    -   Pregled i dodavanje **feedback-a u modalu** (detalji o radu +
        komentari)
    -   Ponovna analiza rada uz dodatne instrukcije
    -   Pregled radova po statusu (Pending, InProgress, Completed)
    -   Praćenje napretka svakog studenta kroz dashboard
-   🛠 **Administrator**:
    -   Upravljanje korisnicima (kreiranje, izmena, brisanje, uloge)
    -   Podešavanje limita za broj predaja u određenom vremenskom
        prozoru (sliding window limiter)
    -   Resetovanje limita (bez ograničenja)
    -   Podešavanje parametara automatske analize
        (aktiviranje/isključivanje tipova evaluacija: grammar,
        plagiarism, code-style...)
    -   Praćenje celog sistema kroz admin dashboard

------------------------------------------------------------------------

## 🏗 Arhitektura

Sistem koristi **mikroservisnu arhitekturu** sa sledećim servisima:

### 🟢 **Backend servisi (C# / .NET 8 + Service Fabric)**

1.  **UserService** (Stateful)
    -   Upravljanje korisnicima (Admin, Student, Professor)
    -   Čuvanje uloga i login podataka u MongoDB
    -   Keširanje korisnika u ReliableDictionary radi bržeg pristupa
    -   API podrška: registracija, login, kreiranje korisnika (admin
        panel)
2.  **SubmissionService** (Stateful)
    -   Upravljanje predajama radova
    -   Čuvanje radova u MongoDB (sa istorijom verzija)
    -   Statusi rada: `Pending`, `InProgress`, `Completed`
    -   Integracija sa EvaluationService za automatsku analizu
    -   Podrška za više formata fajlova (`txt`, `pdf`, `docx`, `zip`,
        `cs`, `cpp`, `py`, ...)
    -   API rute:
        -   `POST /api/submission/submit`
        -   `GET /api/submission/my`
        -   `GET /api/submission/all`
        -   `GET /api/submission/student/{id}`
        -   `GET /api/submission/by-status`
3.  **EvaluationService** (Stateful)
    -   Automatska analiza radova pomoću **LLM (Groq API -- LLaMA 4
        Scout)**
    -   Generisanje:
        -   Ocene
        -   Komentara i grešaka
        -   Sugestija za poboljšanje
    -   Ponovna analiza rada sa instrukcijama profesora
    -   Čuvanje evaluacija u MongoDB
    -   Integracija sa ProgressService
4.  **ProgressService** (Stateless)
    -   Analiza napretka studenata na osnovu istorije radova
    -   Izračunavanje metrika uspeha i preporuka
    -   Slanje podataka ka frontendu (StudentDashboard)
5.  **WebApi Gateway** (Stateless)
    -   API sloj koji povezuje frontend i backend
    -   JWT autentifikacija i autorizacija
    -   Kontroleri:
        -   **UsersController** -- login, registracija, admin CRUD
            korisnika
        -   **SubmissionController** -- predaja i pregled radova
        -   **EvaluationController** -- analiza radova, re-analiza
        -   **AdminController** -- podešavanja limita, parametara
            analize

------------------------------------------------------------------------

### 🎨 **Frontend (React + Vite + Ant Design)**

-   **Struktura:**
    -   `components/` -- Navbar, ProtectedRoute, modalne komponente
    -   `pages/` -- Login, Register, StudentDashboard,
        ProfessorDashboard, AdminDashboard
    -   `services/` -- API servisi (Axios wrapper)
    -   `context/` -- AuthContext sa JWT tokenima
    -   `styles/` -- globalni i modularni CSS
    -   `.env` -- backend URL i API ključevi
-   **Implementirane stranice:**
    -   **Login / Register**
        -   Login i registracija korisnika
        -   JWT token čuvanje i logout
        -   Zaštita ruta (Student, Professor, Admin)
    -   **StudentDashboard**
        -   Upload fajlova (više formata)
        -   Pregled istorije predaja
        -   Pregled feedback-a u modalu
        -   Prikaz napretka (grafikon napretka studenta)
    -   **ProfessorDashboard**
        -   Pregled svih radova svih studenata
        -   Pregled feedback-a
        -   Ponovna analiza sa dodatnim instrukcijama
        -   Dodavanje komentara na rad
    -   **AdminDashboard**
        -   CRUD korisnika (kreiranje, izmjena, brisanje)
        -   Podešavanje limita predaja (sliding window limiter)
        -   Resetovanje limita na "unlimited"
        -   Podešavanje tipova analiza (grammar, plagiarism, code-style,
            complexity...)

------------------------------------------------------------------------

## 🛠 Tehnologije

-   **Backend**:
    -   .NET 8, C#, Microsoft Service Fabric\
    -   MongoDB (trajna pohrana)\
    -   ReliableDictionary (lokalni keš podataka)\
    -   JWT autentifikacija\
    -   SignalR (real-time notifikacije)
-   **Frontend**:
    -   React + Vite\
    -   Ant Design (UI biblioteka)\
    -   Axios (API pozivi)
-   **Analiza radova**:
    -   Integracija sa **Groq API**\
    -   Model: `meta-llama/llama-4-scout-17b-16e-instruct`\
    -   Podrška za višestruke formate fajlova
-   **Deployment**:
    -   Service Fabric cluster (lokalno i u oblaku)

------------------------------------------------------------------------

## ⚙️ Pokretanje projekta

### 1. Kloniranje repozitorijuma

``` bash
git clone https://github.com/MastilovicRadoslav/CRUIS_Auto_Grader.git
cd CRUIS_Auto_Grader
```

### 2. Backend (.NET + Service Fabric)

-   Uđi u `EducationalAnalysisSystem` i pokreni aplikaciju
-   Konfiguriši `appsettings.json` i `.env` fajlove (MongoDB konekcija,
    JWT key, Groq API ključ)
-   Pokreni u Service Fabric klasteru ili lokalno

### 3. Frontend (React + Vite)

``` bash
cd frontend
npm install
npm run dev
```

Frontend se podiže na: `http://localhost:5173`

------------------------------------------------------------------------

## 📊 Primer upotrebe

-   Student se loguje i predaje rad (npr. `.pdf`)
-   EvaluationService automatski poziva LLM i vraća ocenu, listu grešaka
    i sugestije
-   Profesor može da pregleda rad, doda sopstvene komentare i pokrene
    ponovnu analizu sa instrukcijama
-   Student vidi napredak u svom dashboard-u
-   Admin može da ograniči broj predaja i izmeni pravila analize

------------------------------------------------------------------------

## 📌 Status projekta

✅ Backend servisi završeni\
✅ Frontend funkcionalnosti (Student, Profesor, Admin dashboard)
završene\
✅ Integracija sa LLM API završena\
✅ Podrška za različite fajl formate

------------------------------------------------------------------------

## 🔄 Planirano: dodatne analitičke funkcije i poboljšanja UI-a

- Integracija naprednih analitičkih funkcija za dublju evaluaciju edukativnih radova
- Poboljšanja korisničkog interfejsa za intuitivnije korišćenje sistema
- Razmatrana je i implementacija **PostgreSQL** baze podataka zbog skalabilnosti i naprednih mogućnosti analize podataka, 
  ali zbog obima projekta trenutno nije korišćena (odabrana je MongoDB implementacija)
  
------------------------------------------------------------------------

## 👨‍💻 Autor i mentorstvo

-   **Autor**: Radoslav Mastilović\
    Master studije -- Fakultet tehničkih nauka, Novi Sad\
    Smer: Primijenjeno softversko inženjerstvo

-   **Mentor i asistent**: Sladjana Turudić
