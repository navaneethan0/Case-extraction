# eCourts Case Data Extractor

A full-stack web application to extract case details from the Indian eCourts website (services.ecourts.gov.in) using a CNR number and manual CAPTCHA entry.

## Tech Stack
* **Frontend:** React + Vite, Axios, Lucide React
* **Backend:** ASP.NET Core Web API (.NET 8/7)
* **Scraping:** Microsoft Playwright for .NET

## Prerequisites
* [Node.js](https://nodejs.org/en/) (v16+)
* [.NET SDK](https://dotnet.microsoft.com/download) (.NET 7 or 8)

## Setup Instructions

### 1. Backend Setup

1. Open a terminal and navigate to the backend API directory:
   ```bash
   cd backend/ECourtScraperApi
   ```
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Run the application (Playwright Chromium browser will be auto-installed on the first run):
   ```bash
   dotnet run
   ```
4. The backend will start at `http://localhost:5242`.

### 2. Frontend Setup

1. Open a new terminal and navigate to the frontend directory:
   ```bash
   cd frontend
   ```
2. Install Node dependencies:
   ```bash
   npm install
   ```
3. Start the Vite development server:
   ```bash
   npm run dev
   ```
4. Open the displayed local URL (e.g., `http://localhost:5173`) in your browser.

## Usage
1. Wait for the CAPTCHA image to load from the eCourts website.
2. Enter a 16-digit CNR Number (e.g., TN12345678901234).
3. Type the characters shown in the CAPTCHA image.
4. Click "Search Case".
5. Wait for the extraction process to complete. The case details will be displayed in a formatted view.

## Notes
* **CAPTCHA:** eCourts requires session-bound CAPTCHAs. The backend maintains an open Playwright headless browser session per user using an in-memory dictionary.
* **Session Expiry:** Sessions idle for more than 5 minutes are automatically cleaned up to save system resources. If your session expires, simply click the refresh button on the CAPTCHA image.
* **Dynamic Loading:** The scraper utilizes Playwright's `WaitForAsync` strategies to handle the dynamically loaded content from eCourts.
