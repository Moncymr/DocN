# Document Visibility and Sharing Management Implementation

## Overview
This document describes the implementation of a comprehensive document visibility and sharing management system for DocN, allowing users to control who can access their documents through multiple visibility levels and fine-grained sharing options.

## Problem Solved
The original system needed:
1. Better visibility control for documents
2. Ability to share documents with specific users or groups
3. UI to manage document access permissions
4. Support for different visibility levels (Private, Shared, Organization, Public)
5. Ability to change visibility after document upload

## Architecture

### Database Schema

#### New Tables

**UserGroups**
- Represents groups/teams of users
- Fields: Id, Name, Description, IsActive, CreatedAt, UpdatedAt, OwnerId, TenantId
- Supports multi-tenant architecture

**UserGroupMembers**
- Links users to groups
- Fields: Id, GroupId, UserId, Role (Member/Admin), JoinedAt
- Unique constraint on (GroupId, UserId) to prevent duplicates

**DocumentGroupShares**
- Links documents to groups for sharing
- Fields: Id, DocumentId, GroupId, Permission, SharedAt, SharedByUserId
- Supports Read, Write, Delete permissions
- Unique constraint on (DocumentId, GroupId)

### Backend Implementation

#### Models
- `UserGroup.cs` - Group management
- `UserGroupMember.cs` - Group membership
- `DocumentGroupShare.cs` - Document-to-group sharing
- Updated `ApplicationUser` - Added group navigation properties
- Updated `Document` - Added GroupShares collection

#### Services
Extended `DocumentService` with new methods:
- `ShareDocumentWithGroupAsync()` - Share document with a group
- `RemoveUserShareAsync()` - Remove user access
- `RemoveGroupShareAsync()` - Remove group access
- `GetDocumentSharesAsync()` - Get all shares for a document
- Updated `CanUserAccessDocument()` - Check group membership access

#### API Endpoints
New endpoints in `DocumentsController`:
- `PATCH /documents/{id}/visibility` - Update document visibility
- `POST /documents/{id}/shares/user` - Share with specific user
- `POST /documents/{id}/shares/group` - Share with group
- `GET /documents/{id}/shares` - List all shares
- `DELETE /documents/{id}/shares/user/{userId}` - Remove user share
- `DELETE /documents/{id}/shares/group/{groupId}` - Remove group share

### Frontend Implementation

#### ShareDocumentModal Component
A beautiful, modern modal for managing document visibility and sharing:

**Features:**
- Visual visibility level selector with icons and descriptions
- Tabbed interface for user vs. group sharing
- Live display of current shares
- Permission level selection (Read, Write, Delete)
- Ability to remove shares
- Responsive design with smooth animations

**Visibility Levels:**
1. 🔒 **Private** - Only the owner can see
2. 👥 **Shared** - Shared with specific users/groups
3. 🏢 **Organization** - All organization members
4. 🌐 **Public** - Everyone can access

#### Documents Page Updates
- Integrated ShareDocumentModal component
- Updated ShareDocument function to open modal
- Added visibility change button in document details panel
- Visual indicators for current visibility level

## User Flow

### Changing Document Visibility
1. User navigates to Documents page
2. Clicks on a document to view details
3. Clicks the edit icon next to visibility badge
4. ShareDocumentModal opens
5. User selects desired visibility level
6. User clicks "Save Changes"
7. Visibility is updated immediately

### Sharing with Users
1. User opens ShareDocumentModal
2. Sets visibility to "Shared"
3. Switches to "Users" tab
4. Enters user email or name to search
5. Selects permission level (Read/Write/Delete)
6. Clicks "Add"
7. User appears in current shares list
8. Can be removed later if needed

### Sharing with Groups
1. User opens ShareDocumentModal
2. Sets visibility to "Shared"
3. Switches to "Groups" tab
4. Searches for group
5. Selects permission level
6. Clicks "Add"
7. All group members gain access

## Security Considerations

### Access Control
- Only document owners can change visibility
- Only document owners can manage shares
- Group membership is checked when accessing shared documents
- Multi-tenant isolation maintained

### Permission Levels
- **Read**: View and download document
- **Write**: Read + edit document metadata
- **Delete**: Write + delete document

## Database Migration

The migration `AddUserGroupsAndDocumentGroupShares` creates:
- UserGroups table
- UserGroupMembers table
- DocumentGroupShares table
- Necessary foreign keys and indexes
- Unique constraints for data integrity

To apply:
```bash
dotnet ef database update --project DocN.Data --startup-project DocN.Server
```

## Future Enhancements

### Short Term
- [ ] Implement user search API endpoint
- [ ] Implement group search API endpoint
- [ ] Add group management UI (create, edit, delete groups)
- [ ] Add group member management UI

### Medium Term
- [ ] Email notifications when documents are shared
- [ ] Share link generation for external users
- [ ] Expiring shares with time limits
- [ ] Audit log for visibility changes

### Long Term
- [ ] Advanced permission policies
- [ ] Folder-level sharing
- [ ] Bulk sharing operations
- [ ] Share templates for common scenarios

## Testing Checklist

### Backend
- [ ] Test visibility changes persist
- [ ] Test access control enforcement
- [ ] Test group membership access
- [ ] Test share removal
- [ ] Test multi-tenant isolation

### Frontend
- [ ] Test modal opens and closes correctly
- [ ] Test visibility level selection
- [ ] Test tab switching
- [ ] Test responsive design on mobile
- [ ] Test animations and transitions

### Integration
- [ ] Test end-to-end visibility change flow
- [ ] Test sharing with users
- [ ] Test sharing with groups
- [ ] Test removing shares
- [ ] Test permission changes

## Files Modified/Created

### Backend
- `DocN.Data/Models/UserGroup.cs` (NEW)
- `DocN.Data/Models/ApplicationUser.cs`
- `DocN.Data/Models/Document.cs`
- `DocN.Data/ApplicationDbContext.cs`
- `DocN.Data/Services/DocumentService.cs`
- `DocN.Server/Controllers/DocumentsController.cs`
- `DocN.Data/Migrations/20260108043707_AddUserGroupsAndDocumentGroupShares.cs` (NEW)

### Frontend
- `DocN.Client/Components/Pages/ShareDocumentModal.razor` (NEW)
- `DocN.Client/Components/Pages/Documents.razor`

## API Documentation

### Update Document Visibility
```http
PATCH /documents/{id}/visibility
Content-Type: application/json

{
  "visibility": 0  // 0=Private, 1=Shared, 2=Organization, 3=Public
}
```

### Share with User
```http
POST /documents/{id}/shares/user
Content-Type: application/json

{
  "userId": "user-id-string",
  "permission": 0  // 0=Read, 1=Write, 2=Delete
}
```

### Share with Group
```http
POST /documents/{id}/shares/group
Content-Type: application/json

{
  "groupId": 123,
  "permission": 0  // 0=Read, 1=Write, 2=Delete
}
```

### Get Document Shares
```http
GET /documents/{id}/shares

Response:
{
  "userShares": [
    {
      "userId": "...",
      "userName": "...",
      "userEmail": "...",
      "permission": 0,
      "sharedAt": "2026-01-08T..."
    }
  ],
  "groupShares": [
    {
      "groupId": 1,
      "groupName": "...",
      "memberCount": 5,
      "permission": 0,
      "sharedAt": "2026-01-08T..."
    }
  ]
}
```

### Remove User Share
```http
DELETE /documents/{id}/shares/user/{userId}
```

### Remove Group Share
```http
DELETE /documents/{id}/shares/group/{groupId}
```

## Conclusion

This implementation provides a comprehensive, user-friendly system for managing document visibility and sharing in DocN. The combination of flexible backend APIs and an intuitive UI enables users to precisely control document access while maintaining security and multi-tenant isolation.

The system is designed to be extensible, with clear pathways for adding features like email notifications, share links, and advanced permission policies in the future.
