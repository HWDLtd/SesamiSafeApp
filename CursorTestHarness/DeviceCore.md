# Sesami Device Core — what we have learned

Living notes for the emulator at `https://devicecore.io` and the console harness in `Sesami.TestHarness`. Update this file when a workflow, header, or device behaviour is confirmed.

Connection details (host, certificate, whitelist) are in `Sesami_Emulator_Connection_Notes.txt`. Swagger is `https://devicecore.io/swagger` (`/swagger/v1/swagger.json`). The API title is Recycler Api v1.

## How a call is made

Trust the Sesami CA in `ca.cert.zip` before HTTPS. The harness loads `ca.cert.pem` from that zip.

Anonymous calls (type codes, login, `getApplicationConfiguration`) send `Content-Type: application/json` only.

After login, every API call and the SignalR connection send:

- `Authorization: Bearer {userSession.apiToken}`
- `UserSession`: the `userSession` object from the login response, as raw JSON
- `UserPermissions`: JSON array of permission ids. A system user sends every id from `GET /api/v1/type/permissionCodes`

The emulator UI sends the same three headers. Request bodies use PascalCase (`WorkFlowCodeId`, `DeviceTransactions`). Responses are camelCase.

SignalR hub: `/signalr/notificationHub`. Events observed:

- `FinancialTransactionCompletedEvent` — payload is `{ message: { transactionFinancial } }`
- `NoteRecyclerNoteProcessedEvent`
- Also subscribed by the UI, not yet handled here: `HardwareStatusAddedEvent`, `HardwareStatusRemovedEvent`, `PeripheralStatusEvent`, `StorageDoorLockEvent`, `StorageDoorStatusEvent`, `BarcodeScannedEvent`, `NoteRecyclerStorageContainerStatusManipulatedEvent`

## Login response

`POST /api/v1/authentication/login` with username, password, and `UserSessionType` Local (`id` 1). Session types from `GET /api/v1/type/userSessionTypeCodes`: Local 1, Remote 2, CRACN 3, PACN 4, QR 5.

The response is `isSystemUser`, `userSession`, `typeUserSessionTypeCodeId`, `requiresPinChange`. `userSession.person` is a `PersonRecord`. It does **not** include `userTimeout`. Swagger defines `userTimeout` on the full `Person` type, not on the login record. The harness prints any property whose name contains "timeout"; on this emulator that list is empty, so the user timer stays off.

After login the harness prints the session start, session type, whether a PIN change is required, and the full `person` JSON with `password` redacted. On this emulator that user is username `1234`, display name System, first and last name System, language code 1, no email, phone, external id, import id, or vend limits, and no team memberships. `requiresPinChange` is false.

## Device configuration

`GET /api/v1/configuration/getApplicationConfiguration` works before login. The harness dumps `appConfig` at boot and again on Startup. Properties named `key`, `secret`, `password`, and `apiToken` are printed as `***`.

There is no on/off flag for username and password. That login is always offered. The extra methods are:

| Property | On this emulator |
|---|---|
| `authenticationConfiguration.allowNFCLogin` | true |
| `authenticationConfiguration.allowBarcodeLogin` | true |
| `authenticationConfiguration.allowRFIDLogin` | true |
| `authenticationConfiguration.pacnConfiguration.isEnabled` | true |
| `courierConfiguration.cracnConfiguration.isEnabled` | false |

Store seen in config: HWD MOBILE, Dallas. Theme Sesami, language en-US. `generalConfiguration.workFlow` is "Sesami SmartSafe". First-time setup is already complete. Active template is **Sesami Recycler v1.0.2**.

## How workflows are chained

The client starts one workflow. The backend returns a `workFlowExecutionContext`. The client runs only the stage named there, then completes that stage. The response is the next context. The client does not choose the next screen.

Fields that matter: `currentWorkFlowCodeId`, `currentWorkFlowStageCodeId`, `currentWorkFlowStageInstanceId`, `currentWorkFlowInstanceId`, `currentPosition`, `isWorkFlowComplete`.

- `POST /api/v1/workflow/workFlowStart` — body `WorkFlowCodeId`, `UserSessionId`, `PersonId`
- `POST /api/v1/workflow/workFlowStageComplete` — body `WorkFlowStageInstanceId`, `WorkFlowStageData` (a JSON string)
- `POST /api/v1/workflow/workFlowAbort` — body stage id, `WorkFlowExecutionStatusCodeId`, reason
- `GET /api/v1/workflow/getIncompleteWorkFlow/{userId}` — open chain for that person, or `workFlowResume: null`
- `POST /api/v1/workflow/workFlowAbortOrphans` — startup cleanup of workflows with no user

Other chain calls, used by the emulator UI and not yet driven here: `workFlowGoBack`, `workFlowJump`, `workFlowException`, `workFlowAbortAll`. Jump and exception spawn another workflow off the current stage (spawn codes: Template 1, Jump 2, Exception 3). Abort can return `isJumpResume`, which means control went back to the parent chain.

Execution status codes: Started 1, Completed 2, Aborted 3, TimedOut 4, Suspended 5, Skipped 6, Return 7.

A stage the harness does not recognise is left open. Nothing is completed or aborted.

## Workflow numbers we use

| Code | Name | What the harness does |
|---|---|---|
| 3001 | Login | Start and complete. One stage, **6001**. Recorded for history. Does not choose the next screen. |
| 3002 | Logout | Start and complete stage **6002**, then `POST /api/v1/authentication/logout`. |
| 1004 | Add Cash | Stage **1000** Accept Cash, then stage **1007** Print Receipt. Same workflow the whole way. Completing 1007 sets `isWorkFlowComplete`. |
| 1 | FirstTimeSetup | Not run. Config says setup is already complete. The UI would start this for a system user if it were not. |

Menu and log lines print these numbers on every workflow action.

## Accept Cash (workflow 1004, stage 1000)

1. `POST /api/v1/transaction/createAcceptTransactions` for components `BNR`, `BCR`, `SS`, transaction type Deposit (id 3). Accounting period is the first open period when one exists.
2. The returned `deviceTransactions` are the open set. On this emulator that is one Smart Safe (`componentCode` SS, asset `adb449ce-2763-41a7-8cae-3999f21ae3aa`). Note recycler `isEnabled` is false, so BNR/BCR do not start.
3. Subscribe to SignalR **before** `POST /api/v1/recycler/startAcceptCash`. Body is `{ DeviceTransactions: <still open> }`.
4. A device leaves the open set only when its `FinancialTransactionCompletedEvent` arrives. Match `componentId` when both sides have it, otherwise `assetId`.
5. `POST /api/v1/recycler/stopAcceptCash` is sent only for devices still open. It does not complete the stage.
6. `S` requests stop. `Q` aborts the stage with status 3. A keypress resets the user timer.
7. When the set is empty, `workFlowStageComplete` sends `{ completedTransactions: [transactionFinancial, ...] }`.

`transactionError` on `transactionFinancial` is null when the device finished cleanly. When it is set, the console prints that id and `transactionStatus` if present. The device still counts as closed. The stage still completes after every device has reported. An error does not leave hardware open and does not skip the rest of the chain.

### Three timeouts

- **User timeout.** How long someone may sit on a screen. Read from `userSession.person.userTimeout` on the login response. Not present on this emulator, so the timer is off. If it fires, abort with status 4 and log out.
- **Stage timeout.** `timeoutDuration` on the stage configuration for stage 1000 in the active template. This template sends `"0"`, which means the timer is off. When it is greater than zero, call `stopAcceptCash` for devices still open, then keep waiting for their completion events.
- **Hardware timeout.** Not a timer in the harness. The device ends acceptance and publishes `FinancialTransactionCompletedEvent`. Treat that like stop for that device only. Do not call `stopAcceptCash` again for a device that has already completed.

## Print Receipt (workflow 1004, stage 1007)

Not a new workflow. Completing stage 1000 returns a context whose workflow is still 1004 and whose stage is 1007.

`POST /api/v1/receipt/addCashReceipt` with the workflow instance id, the Add Cash type-code record, and the **Deposit** `transactionFinancial` objects. `IsPrinted`, `IsStored`, and `NumberOfPrints` 1. This emulator returns HTTP 503 `NoReceiptPrinters`. The UI still completes the stage, and so does the harness. That completion finishes workflow 1004.

## Manual drop

Not its own workflow. It is a button on the Accept Cash screen.

Shown only when `hardwareConfiguration.dropVaultConfiguration.isEnabled` is true and the user has workflow **1010** Create Device Bag or **1011** Create Bag.

The button sets `performManualDrop` and then calls `stopAcceptCash` (or completes immediately if acceptance never started). Stage data sent to the backend is the completed transactions plus `performManualDrop: true`. The backend decides the next stage.

Related, but separate:

- **1009** Empty Drop Vault. Disabled on the emulator flow-select menu (`enabled: false`). Screen calls `POST /api/v1/transaction/createEmptyDropVaultTransaction` with component code `DV`, then `POST /api/v1/dropVault/emptyDropVault`.
- On the receipt screen, drop-vault notes are transaction type Adjustment and subtype ManualDrop. They are shown as a manual-drop amount. The receipt API call only sends Deposit transactions.

The harness does not send `performManualDrop`. `S` only stops acceptance.

## Harness commands

```text
dotnet run --project Sesami.TestHarness
dotnet run --project Sesami.TestHarness -- session
```

No arguments opens the menu. Commands: `startup`, `login`, `incomplete`, `abort`, `accept-cash`, `logout`, `session`.

Environment: `SESAMI_BASE_URL`, `SESAMI_USERNAME`, `SESAMI_PASSWORD`, `SESAMI_CA_CERT`.

## Not driven yet

Check-in 1000, check-out 1001, loan 1002, pickup 1003, buy change 1005, retract 1006, prepare deposit 1007, and the rest of the template. Stage codes for those exist (`DispenseMix` 1001, `RetractCash` 1002, `PrepareDeposit` 1003, and so on) but the harness stops and leaves the workflow open if the backend returns one.
