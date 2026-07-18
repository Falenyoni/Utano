# File Storage — Utano.Module.Files

## Overview

Utano uses **Cloudflare R2** as its file storage backend, accessed via the S3-compatible API. Files are uploaded directly from the browser — they never pass through the application server — keeping bandwidth costs near zero and server load minimal.

Supported file types: JPEG, PNG, WebP, GIF, PDF, DICOM (X-ray format).  
Maximum file size: **50 MB** per file.

---

## Why Cloudflare R2

| Concern | Decision |
|---|---|
| Egress fees | R2 charges **zero egress** — critical for medical images opened frequently |
| API compatibility | S3-compatible — uses `AWSSDK.S3` with a custom endpoint, no bespoke SDK |
| Cost at clinic scale | Free tier covers 10 GB + 1 M requests/month — enough for several pilot practices |
| Data residency | R2 has a Johannesburg point-of-presence, minimising latency for ZW/SA clients |

> **Render Persistent Disks are not used.** They are tied to a single instance and break under horizontal scaling. R2 is the correct solution for shared, durable file storage.

---

## Module Structure

```
Utano.Module.Files/
├── Configuration/
│   └── AppConfiguration.cs          # DI registration + module wiring
├── DatabaseMappings/
│   ├── FilesDbContext.cs             # EF context with practice-scoped query filter
│   └── FileAttachmentConfiguration.cs
├── Domain/
│   ├── Entities/
│   │   └── FileAttachment.cs        # Aggregate root
│   ├── Enums/
│   │   └── FileAttachmentType.cs
│   └── Interfaces/
│       ├── IFileAttachmentRepository.cs
│       └── IFileStorageService.cs   # Storage abstraction (swap R2 for anything)
├── Features/
│   └── Files/
│       ├── RequestUploadUrl/        # POST /api/files/upload-url
│       ├── GetDownloadUrl/          # GET  /api/files/{id}/url
│       ├── ListFiles/               # GET  /api/files?patientId={}&type={}
│       └── DeleteFile/              # DELETE /api/files/{id}
├── Infrastructure/
│   ├── Repositories/
│   │   └── FileAttachmentRepository.cs
│   └── Services/
│       └── R2FileStorageService.cs  # IFileStorageService implementation
└── Migrations/
    └── (EF migrations)
```

---

## Domain Entity — FileAttachment

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `PracticeId` | `Guid` | Tenant scope — enforced via EF query filter |
| `PatientId` | `Guid` | Required — file always belongs to a patient |
| `ConsultationId` | `Guid?` | Optional link to a clinical consultation |
| `FileName` | `string` | Original filename shown in the UI |
| `ObjectKey` | `string` | R2 storage path — never exposed to clients directly |
| `ContentType` | `string` | MIME type validated on upload request |
| `SizeBytes` | `long` | Validated ≤ 50 MB |
| `AttachmentType` | `FileAttachmentType` | See enum below |
| `Description` | `string?` | Optional clinical note |
| `IsDeleted` | `bool` | Soft-delete flag — also applied in query filter |

**FileAttachmentType enum:**
```
XRay | LabResult | ClinicalNote | Referral | Prescription | ConsentForm | InsuranceDocument | Other
```

**Object key format:**  
`{practiceId}/{patientId}/{attachmentType}/{uuid}.{ext}`  
Example: `a1b2c3.../d4e5f6.../xray/9f8e7d6c.jpg`

Tenant isolation is enforced at two levels: the EF query filter (`PracticeId == currentUser.PracticeId && !IsDeleted`) and the object key structure in R2.

---

## API Endpoints

### POST `/api/files/upload-url`

Registers the file metadata and returns a presigned PUT URL for direct browser upload to R2.

**Request body:**
```json
{
  "patientId": "guid",
  "fileName": "chest-xray.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 2097152,
  "attachmentType": "XRay",
  "consultationId": "guid | null",
  "description": "Chest PA view — 2026-07-18"
}
```

**Response:**
```json
{
  "fileId": "guid",
  "uploadUrl": "https://{accountId}.r2.cloudflarestorage.com/...",
  "contentType": "image/jpeg"
}
```

The client must include `Content-Type: image/jpeg` (matching `contentType`) when making the PUT request to `uploadUrl`. The URL expires in **5 minutes**.

### GET `/api/files/{id}/url`

Returns a presigned GET URL for downloading or displaying the file. Expires in **60 minutes**.

```json
{
  "downloadUrl": "https://...",
  "fileName": "chest-xray.jpg",
  "contentType": "image/jpeg"
}
```

### GET `/api/files?patientId={guid}&type={FileAttachmentType}`

Returns file metadata for a patient. `type` is optional. Does **not** return download URLs — call `GET /api/files/{id}/url` per file when access is needed.

```json
[
  {
    "id": "guid",
    "patientId": "guid",
    "consultationId": "guid | null",
    "fileName": "chest-xray.jpg",
    "contentType": "image/jpeg",
    "sizeBytes": 2097152,
    "attachmentType": "XRay",
    "description": "Chest PA view",
    "createdAt": "2026-07-18T09:30:00Z"
  }
]
```

### DELETE `/api/files/{id}`

Soft-deletes the metadata record and permanently removes the object from R2. Returns `204 No Content`.

---

## Upload Flow (Sequence)

```
Browser                     Utano API                    Cloudflare R2
  │                              │                              │
  │── POST /api/files/upload-url ──>                           │
  │                              │── creates FileAttachment     │
  │                              │── generates presigned PUT ──>│
  │<── { fileId, uploadUrl } ────│                              │
  │                              │                              │
  │── PUT uploadUrl (binary) ──────────────────────────────────>│
  │<── 200 OK ──────────────────────────────────────────────────│
  │                              │                              │
  │  (optionally confirm upload) │                              │
```

---

## Frontend Integration

When rendering a file:
1. Call `GET /api/files/{id}/url` to get a fresh presigned download URL.
2. Use the URL as `src` for `<img>` (images) or `href` for a download link.
3. For PDFs, render inline using an `<iframe src={downloadUrl}>` or a PDF viewer component.
4. Do not cache the URL client-side beyond the page session — it expires in 60 minutes.

For the upload dropzone:
1. Collect file from the user (`File` object).
2. Call `POST /api/files/upload-url` with metadata.
3. `fetch(uploadUrl, { method: 'PUT', body: file, headers: { 'Content-Type': file.type } })`.
4. On success, show the returned `fileId` in state — the file is now accessible.

---

## Configuration

Add to `appsettings.json` (use user secrets or environment variables in production):

```json
"FileStorage": {
  "AccountId": "your-cloudflare-account-id",
  "AccessKeyId": "your-r2-access-key-id",
  "SecretAccessKey": "your-r2-secret-access-key",
  "BucketName": "utano-files",
  "UploadUrlExpiryMinutes": 5,
  "DownloadUrlExpiryMinutes": 60
}
```

**Getting R2 credentials:**
1. Cloudflare Dashboard → R2 → Create bucket (`utano-files`)
2. R2 → Manage R2 API Tokens → Create API Token → `Object Read & Write`
3. Copy Account ID, Access Key ID, Secret Access Key

**Bucket settings:**
- Public access: **off** (all access via presigned URLs only)
- CORS: add your frontend origin with `PUT` and `GET` allowed

---

## Running the Migration

```bash
cd src/Utano.API

dotnet ef database update \
  --context FilesDbContext \
  --project ../Modules/Utano.Module.Files/Utano.Module.Files.csproj \
  --startup-project .
```

---

## Security Notes

- **Object keys are never returned to clients.** Clients only receive `fileId` (database Guid) and time-limited presigned URLs.
- **Tenant isolation** is enforced by both the EF query filter on `PracticeId` and the object key prefix. A user from practice A cannot construct a valid presigned URL for practice B's files.
- **Allowed content types** are validated server-side before the presigned URL is generated. Uploads of unexpected types are rejected before they reach R2.
- **Soft delete** marks records as deleted in the database and removes the object from R2. The `IsDeleted` flag is part of the EF global query filter, so deleted files are invisible to all subsequent queries.
