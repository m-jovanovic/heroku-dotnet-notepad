# Real-Time Sticky Notes App

A real-time collaborative sticky notes application built with ASP.NET Core and SignalR. Users can create, edit, move, and delete notes that update in real-time for all connected users.

## Features

- Create, edit, and delete sticky notes
- Real-time updates using SignalR
- Drag and drop notes to reposition them
- Customize note colors
- Responsive design
- Collaborative editing

## Local Development

1. Make sure you have .NET 8.0 SDK installed
2. Clone the repository
3. Navigate to the project directory
4. Run the application:
   ```bash
   dotnet run
   ```
5. Open your browser and navigate to `https://localhost:5001`

## Deployment to Heroku

1. Install the Heroku CLI
2. Login to Heroku:
   ```bash
   heroku login
   ```
3. Create a new Heroku app:
   ```bash
   heroku create your-app-name
   ```
4. Deploy to Heroku:
   ```bash
   git push heroku main
   ```
5. Open your app:
   ```bash
   heroku open
   ```

## Technologies Used

- ASP.NET Core 8.0
- SignalR
- HTML5
- CSS3
- JavaScript
- Bootstrap 5 