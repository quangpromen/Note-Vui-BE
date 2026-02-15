# 📱 Mobile API Documentation - NoteVui

This documentation provides detailed JSON payloads and endpoint specifications for the NoteVui Android/Mobile client. 

---

## 🚀 General Information
- **Base URL:** `https://api.notevui.com/api` (Production) or `http://10.0.2.2:5000/api` (Android Emulator)
- **Auth:** `Bearer {Token}` in the `Authorization` header for all protected endpoints.
- **Content-Type:** `application/json`

---

## 🔐 Authentication & Account (`api/auth`)

### 1. Register
Create a new user account.
- **Method:** `POST`
- **Endpoint:** `/api/auth/register`
- **Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecretPassword123!",
  "fullName": "Nguyen Van A"
}
```
- **Success Response (200 OK):**
```json
{
  "accessToken": "ey...",
  "refreshToken": "...",
  "userId": "guid-string",
  "email": "user@example.com",
  "fullName": "Nguyen Van A",
  "avatarUrl": null
}
```

### 2. Login
Authenticate and receive tokens.
- **Method:** `POST`
- **Endpoint:** `/api/auth/login`
- **Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecretPassword123!"
}
```
- **Success Response (200 OK):** Same as Register.

### 3. Refresh Token
Get a new AccessToken when the old one expires.
- **Method:** `POST`
- **Endpoint:** `/api/auth/refresh-token`
- **Request Body:**
```json
{
  "accessToken": "expired-access-token",
  "refreshToken": "current-refresh-token"
}
```
- **Success Response (200 OK):** New `accessToken` and `refreshToken`.

### 4. Update Profile
- **Method:** `PUT`
- **Endpoint:** `/api/auth/profile`
- **Request Body:**
```json
{
  "fullName": "New Name",
  "avatarUrl": "https://image-url.com/avatar.jpg"
}
```

### 5. Change Password
- **Method:** `POST`
- **Endpoint:** `/api/auth/change-password`
- **Request Body:**
```json
{
  "currentPassword": "old-password",
  "newPassword": "new-password-123"
}
```

---

## � Synchronization (`api/sync`)
*This is the primary endpoint for mobile apps to keep local data in sync with the server.*

- **Method:** `POST`
- **Endpoint:** `/api/sync`
- **Request Body (`SyncRequest`):**
```json
{
  "lastSyncTime": "2024-02-08T10:00:00Z", 
  "changes": [
    {
      "clientId": "550e8400-e29b-41d4-a716-446655440000",
      "noteId": 12,
      "title": "Meeting Notes",
      "shortPreview": "Discussing project X...",
      "fullContent": "Full content of the meeting...",
      "isPinned": true,
      "isDeleted": false,
      "createdAt": "2024-01-01T08:00:00Z",
      "updatedAt": "2024-02-08T11:30:00Z"
    }
  ]
}
```
- **Success Response (200 OK):**
```json
{
  "upserts": [
    {
      "clientId": "eb72-...",
      "noteId": 15,
      "title": "Server Updated Note",
      "shortPreview": "...",
      "fullContent": "...",
      "isPinned": false,
      "isDeleted": false,
      "createdAt": "2024-02-08T09:00:00Z",
      "updatedAt": "2024-02-08T12:00:00Z"
    }
  ],
  "serverTime": "2024-02-08T12:05:00Z",
  "stats": {
    "clientChangesReceived": 1,
    "inserted": 0,
    "updated": 1,
    "conflicts": 0,
    "serverChangesReturned": 1
  }
}
```
*Note: Mobile should store `serverTime` as the next `lastSyncTime`.*

---

## � Cloud Notes (`api/notes`)
*Use these for direct online operations or single-note management.*

### 1. List Notes
- **Method:** `GET`
- **Endpoint:** `/api/notes?search=abc&pageIndex=1&pageSize=10`
- **Success Response (200 OK):** `List<NoteDto>` (See Model section).

### 2. Create Note
- **Method:** `POST`
- **Endpoint:** `/api/notes`
- **Request Body:**
```json
{
  "title": "New Note",
  "shortPreview": "Preview...",
  "fullContent": "Large text content...",
  "isPinned": false
}
```

### 3. Update Note
- **Method:** `PUT`
- **Endpoint:** `/api/notes/{id}`
- **Request Body:** Same as Create Note.

### 4. Delete/Restore Note
- **DELETE** `/api/notes/{id}`: Move to trash.
- **PATCH** `/api/notes/{id}/restore`: Restore from trash.

---

## 🤖 AI Features (`api/ai`)
*Exclusive to VIP members.*

### 1. General AI Request (Summarize, Grammar, Ideas)
- **Method:** `POST`
- **Endpoints:** `/api/ai/summarize`, `/api/ai/grammar`, `/api/ai/ideas`
- **Request Body:**
```json
{
  "content": "The text you want AI to process...",
  "noteId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 2. Translate
- **Method:** `POST`
- **Endpoint:** `/api/ai/translate`
- **Request Body:**
```json
{
  "content": "Hello world",
  "targetLanguage": "vi",
  "noteId": "..."
}
```

### 3. AI Response Body
- **Success Response (200 OK):**
```json
{
  "result": "Processed text from AI...",
  "isSuccess": true,
  "errorMessage": null,
  "inputTokens": 150,
  "outputTokens": 80,
  "remainingQuota": 9999
}
```

### 4. AI Quota
- **Method:** `GET`
- **Endpoint:** `/api/ai/quota`
- **Response:**
```json
{
  "dailyLimit": 2147483647,
  "usedToday": 5,
  "remaining": 2147483647,
  "isVip": true,
  "resetTime": "2024-02-09T00:00:00.0000000Z"
}
```

---

## 💎 Subscription (`api/subscription`)

### 1. Quick Status
- **Method:** `GET`
- **Endpoint:** `/api/subscription/status`
- **Response:**
```json
{
  "isVip": true,
  "planType": "PremiumMonthly",
  "status": "Active",
  "startDate": "2024-02-01T10:00:00Z",
  "endDate": "2024-03-01T10:00:00Z",
  "daysRemaining": 22,
  "isAutoRenew": false
}
```

### 2. Detailed Info
- **Method:** `GET`
- **Endpoint:** `/api/subscription/details`
- **Response:**
```json
{
  "hasSubscription": true,
  "subscription": {
    "id": 101,
    "userId": "...",
    "planType": 1,
    "status": 1,
    "startDate": "...",
    "endDate": "...",
    "isAutoRenew": false,
    "createdAt": "...",
    "updatedAt": "..."
  }
}
```

---

## 👤 User Profile (`api/user`)

### 1. Get My Profile
Get complete profile information including user info, subscription plan, note counts, and AI usage statistics.
- **Method:** `GET`
- **Endpoint:** `/api/user/profile`
- **Auth Required:** ✅ Bearer Token
- **Success Response (200 OK):**
```json
{
  "userId": "guid-string",
  "email": "user@example.com",
  "fullName": "Nguyen Van A",
  "avatarUrl": "https://image-url.com/avatar.jpg",
  "subscription": {
    "planName": "Premium (Tháng)",
    "planType": "PremiumMonthly",
    "isVip": true,
    "status": "Active",
    "startDate": "2024-02-01T10:00:00Z",
    "endDate": "2024-03-01T10:00:00Z",
    "daysRemaining": 22,
    "isAutoRenew": false
  },
  "totalNotesBackedUp": 45,
  "activeNotes": 40,
  "aiUsage": {
    "usedToday": 5,
    "usedThisMonth": 38,
    "usedThisYear": 120,
    "totalUsed": 120,
    "todayByAction": [
      { "actionType": "Summarize", "count": 2 },
      { "actionType": "FixGrammar", "count": 1 },
      { "actionType": "Translate", "count": 2 }
    ]
  }
}
```
- **Free User Response Example:**
```json
{
  "userId": "guid-string",
  "email": "freeuser@example.com",
  "fullName": "Free User",
  "avatarUrl": null,
  "subscription": {
    "planName": "Free",
    "planType": "Free",
    "isVip": false,
    "status": null,
    "startDate": null,
    "endDate": null,
    "daysRemaining": null,
    "isAutoRenew": false
  },
  "totalNotesBackedUp": 10,
  "activeNotes": 8,
  "aiUsage": {
    "usedToday": 0,
    "usedThisMonth": 0,
    "usedThisYear": 0,
    "totalUsed": 0,
    "todayByAction": []
  }
}
```

| Field | Type | Description |
| :--- | :--- | :--- |
| `userId` | String | User's unique identifier |
| `email` | String | User's email address |
| `fullName` | String | User's display name |
| `avatarUrl` | String? | User's avatar URL (nullable) |
| `subscription.planName` | String | Display name: "Free", "Premium (Tháng)", "Premium (Năm)" |
| `subscription.planType` | String | Enum: "Free", "PremiumMonthly", "PremiumYearly" |
| `subscription.isVip` | Bool | Whether user has active premium subscription |
| `subscription.status` | String? | "Active", "Cancelled", "Expired", or null |
| `subscription.daysRemaining` | Int? | Days until subscription expires |
| `totalNotesBackedUp` | Int | Total notes synced to server (including deleted) |
| `activeNotes` | Int | Notes that are not soft-deleted |
| `aiUsage.usedToday` | Int | AI requests made today |
| `aiUsage.usedThisMonth` | Int | AI requests made this month |
| `aiUsage.usedThisYear` | Int | AI requests made this year |
| `aiUsage.totalUsed` | Int | All-time AI request count |
| `aiUsage.todayByAction` | Array | Breakdown by action type for today |

### 2. Edit My Profile
Update the current user's profile. Returns the full updated profile.
- **Method:** `PUT`
- **Endpoint:** `/api/user/profile`
- **Auth Required:** ✅ Bearer Token
- **Request Body:**
```json
{
  "fullName": "Nguyen Van B",
  "avatarUrl": "https://image-url.com/new-avatar.jpg"
}
```
- **Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Cập nhật thông tin thành công",
  "data": {
    "userId": "guid-string",
    "email": "user@example.com",
    "fullName": "Nguyen Van B",
    "avatarUrl": "https://image-url.com/new-avatar.jpg",
    "subscription": { ... },
    "totalNotesBackedUp": 45,
    "activeNotes": 40,
    "aiUsage": { ... }
  }
}
```
- **Notes:**
  - `fullName`: Required, max 100 characters
  - `avatarUrl`: Optional - send `null` to keep current avatar
  - User **cannot** change their email (only Admin can)

---

## 🛠 Shared Models (JSON Reference)

### `NoteDto` / `NoteSyncDto`
| Field | Type | Description |
| :--- | :--- | :--- |
| `clientId` | UUID | **Primary Key for Mobile**. Must be unique per note. |
| `noteId` | Int? | Server identity. May be null for new local notes. |
| `title` | String | Note title (Max 200/255 chars). |
| `shortPreview`| String? | Short text snippet. |
| `fullContent` | String? | Markdown or Plain text content. |
| `isPinned` | Bool | Pinned status. |
| `isDeleted` | Bool | Soft-delete status. |
| `createdAt` | ISO8601 | Creation time (UTC). |
| `updatedAt` | ISO8601 | Last modification time (UTC). |

---

## ⚠️ Common Error Codes
- `401 Unauthorized`: Missing or invalid Bearer token.
- `403 Forbidden`: VIP feature accessed by Free user.
- `400 BadRequest`: Validation failed (e.g., email format, empty content).
- `404 NotFound`: Resource (Note) not found.
- `500 Internal Server Error`: Server-side crash.
