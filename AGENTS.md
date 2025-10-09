# RefNotes Project

## Project Overview

This is a note-taking application called **RefNotes**. It's a full-stack web application with a .NET backend and an Angular frontend.

- **Backend:** The backend is built with .NET, ASP.NET Core, and Entity Framework Core. It uses a MySQL database for data storage and Redis for caching. The backend API is located in the `src/Api` directory.
- **Frontend:** The frontend is an Angular single-page application (SPA) located in the `src/Web` directory. It uses Tailwind CSS for styling.
- **Containerization:** The project is fully containerized using Docker and Docker Compose. The `compose.yaml` file defines the services for the backend, frontend, database, and a Caddy reverse proxy.

## Building and Running

### With Docker

The easiest way to run the project is with Docker Compose:

```bash
docker-compose up -d
```

This will build the containers and start all the services in the background. The application will be available at `http://localhost`.

### Without Docker

To run the project without Docker, you'll need to have the .NET SDK, Node.js, and pnpm installed.

1.  **Start the backend:**
    ```bash
    cd src/Api
    dotnet watch run
    ```
2.  **Start the frontend:**
    ```bash
    cd src/Web
    pnpm install
    pnpm start
    ```

The application will be available at `http://localhost:4200`.

## Testing

### Backend Tests

To run the backend tests, navigate to the `src/Api` directory and run:

```bash
dotnet test
```

### Frontend Tests

To run the frontend tests, navigate to the `src/Web` directory and run:

```bash
pnpm test
```

## Development Conventions

- **Backend:** The backend follows standard .NET conventions. It uses a service-oriented architecture with controllers, services, and repositories.
- **Frontend:** The frontend follows standard Angular conventions. It uses a component-based architecture with services for data access.
- **Code Style:** The project uses `.editorconfig` to enforce consistent code style.
- **Git:** The project uses Git for version control. Please follow standard Git best practices when contributing.
