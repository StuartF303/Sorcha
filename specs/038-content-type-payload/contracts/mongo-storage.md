# Contract: MongoDB Storage Optimization

**Purpose**: Internal BSON Binary storage for MongoRegisterRepository.

## Internal Document Types

These types exist ONLY within `Sorcha.Register.Storage.MongoDB` and are not exposed via `IRegisterRepository`.

### MongoTransactionDocument

Maps 1:1 to TransactionModel but with binary fields as `byte[]`.

```
Signature: byte[]  (BSON Binary subtype 0x00)
Payloads: MongoPayloadDocument[]
(all other TransactionModel fields: unchanged types)
```

### MongoPayloadDocument

Maps 1:1 to PayloadModel but with binary data fields as `byte[]`.

```
Type: int
Data: byte[]  (BSON Binary subtype 0x00)
Hash: byte[]  (BSON Binary subtype 0x00)
IV: MongoChallengeDocument?
Challenges: MongoChallengeDocument[]?
ContentType: string?
ContentEncoding: string?
```

### MongoChallengeDocument

```
Data: byte[]?  (BSON Binary subtype 0x00)
Address: string?
```

## Conversion Contract

### Write Path (PayloadModel → MongoPayloadDocument)

```
PayloadModel.Data (string, base64url) → Base64Url.Decode → byte[] → BSON Binary
PayloadModel.Hash (string, base64url) → Base64Url.Decode → byte[] → BSON Binary
PayloadModel.IV.Data (string, base64url) → Base64Url.Decode → byte[] → BSON Binary
Challenge.Data (string, base64url) → Base64Url.Decode → byte[] → BSON Binary
PayloadModel.ContentType → string (pass-through)
PayloadModel.ContentEncoding → string (pass-through)
```

### Read Path (MongoPayloadDocument → PayloadModel)

For BSON Binary fields:
```
byte[] → Base64Url.Encode → string → PayloadModel.Data
```

For legacy BsonString fields:
```
string → PayloadModel.Data (preserve as-is, legacy Base64)
```

### Legacy Detection

```
if (bsonValue.IsBsonBinaryData) → new format, decode as byte[]
if (bsonValue.IsString) → legacy format, use string as-is
```
