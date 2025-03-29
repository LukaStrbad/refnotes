# RefNotes

## Overview

RefNotes is a note-taking application built with .NET and Angular.

## Features

- Create, edit, and delete notes.
- File upload
- Organize files into folders and subfolders.
- Markdown preview.
- Tag files.
- Multilingual support.
- Switch between light and dark themes, along with other settings.
- Preview for image files

## Planned Features

- File operations (rename, move)
- File downloads
- Alerts for errors and other actions
- File and folder search
- User groups
- File sharing
- File favorites
- Public notes with a preview URL
- Autocomplete for image paths

## Getting Started

1. Clone the repository.
2. Start the backend server with `dotnet watch` inside the `Server` folder.
3. Start the frontend server with `pnpm start` inside the `Frontend` folder.

## Usage

- Start the backend and frontend servers.
- Navigate to http://localhost:4200.
- Log in and begin using the application.

## Testing

### Running Tests

- For backend tests, navigate to the `Server` folder and run:
  `dotnet test`
- For frontend tests, navigate to the `Frontend` folder and run:
  `pnpm test`

## Screenshots

### Home

![home](images/home.png)

### Login

![login](images/login.png)

### Settings

![settings](images/settings.png)

### Editor

![editor](images/editor.png)
