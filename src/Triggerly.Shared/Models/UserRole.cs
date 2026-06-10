namespace Triggerly.Shared.Models;

// Stored as int in DB: Preparer=0, Reviewer=1, Manager=2
// Migration: Viewer(0)→Preparer(0), Approver(1)→Reviewer(1), Editor(2)→Preparer(0), Admin(3)→Manager(2)
public enum UserRole { Preparer = 0, Reviewer = 1, Manager = 2 }
