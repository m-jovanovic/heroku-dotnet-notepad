using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NotepadApp.Models;
using NotepadApp.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics;

namespace NotepadApp.Hubs
{
    public class NoteHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NoteHub> _logger;
        private static readonly ActivitySource _activitySource = new("NotepadApp");

         public NoteHub(ApplicationDbContext context, ILogger<NoteHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            // Send all existing notes to the newly connected client
            var notes = await _context.Notes.ToListAsync();
            foreach (var note in notes)
            {
                await Clients.Caller.SendAsync("ReceiveNote", note);
            }
            await base.OnConnectedAsync();
        }

        public async Task AddNote(Note note)
        {
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();
            
            await Clients.All.SendAsync("ReceiveNote", note);
            _logger.LogInformation("Starting activity");
            using var activity = _activitySource.StartActivity("NoteHub_AddNote");
            _logger.LogInformation("Completing activity");
            activity?.Stop();
        }

        public async Task UpdateNote(Note note)
        {
            var existingNote = await _context.Notes.FindAsync(note.Id);
            if (existingNote != null)
            {
                existingNote.Title = note.Title;
                existingNote.Content = note.Content;
                existingNote.Color = note.Color;
                existingNote.PositionX = note.PositionX;
                existingNote.PositionY = note.PositionY;
                existingNote.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                await Clients.All.SendAsync("UpdateNote", existingNote);
            }
        }

        public async Task DeleteNote(int noteId)
        {
            var note = await _context.Notes.FindAsync(noteId);
            if (note != null)
            {
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
                await Clients.All.SendAsync("DeleteNote", noteId);
            }
        }

        public async Task MoveNote(int noteId, double x, double y)
        {
            var note = await _context.Notes.FindAsync(noteId);
            if (note != null)
            {
                note.PositionX = x;
                note.PositionY = y;
                note.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                await Clients.All.SendAsync("MoveNote", noteId, x, y);
            }
        }
    }
} 