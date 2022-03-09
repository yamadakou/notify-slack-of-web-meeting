# notify-slack-of-web-meeting

[![Build](https://github.com/yamadakou/notify-slack-of-web-meeting/actions/workflows/build.yml/badge.svg)](https://github.com/yamadakou/notify-slack-of-web-meeting/workflows/build.yml)

* 当日の Web 会議の情報を Slack に通知するWeb サービスです。

## 概要

### Notify Slack of web meeting の特徴

* テレワークが増え会議も Web 会議が主流となり、毎日、Web 会議の URL を Outlook から参照する手間が増えました。
* そこで、今日、予定されている Web 会議を毎朝 Slack で確認できるよう、本サービスでは以下の機能を用意します。
  * Web会議情報を登録・検索・削除する REST API
  * 通知先のSlackチャンネル情報を登録・検索・削除する REST API
  * 朝9時に当日のWeb会議情報を指定の Slack チャンネルに通知する定期バッチ
* Web会議情報や通知先の Slack チャンネル情報の登録などは自由にクライアントを用意することで、 Outlook や Google カレンダーなど好みの予定表から Web 会議情報を抽出し、指定した Slack チャンネルに通知することが可能です。
  * Outlook クライアントからログインユーザーの翌日の Web 会議情報を登録するコンソールアプリは下記リポジトリで提供しており、 Windows タスクスケジューラで毎日実行するよう登録することで、自動的に毎朝9時に当日の Web 会議情報を Slack で確認できます。
    * [notify-slack-of-web-meeting.cli](https://github.com/yamadakou/notify-slack-of-web-meeting.cli)
      * https://github.com/yamadakou/notify-slack-of-web-meeting.cli

### 機能説明
####  Web会議情報を登録・検索・削除する REST API
* Web会議情報を登録
  ```json
  POST api/WebMeetings
  {
      "name": <Web会議名>,
      "startDateTime": <翌日以降のWeb会議の開始日時>,
      "url": <Web会議のURL>,
      "registeredBy": <登録者>,
      "slackChannelId": <通知先のSlackチャンネル情報のID>
  }
  ```
  * 全ての項目が必須項目となります。
  * `startDateTime` は翌日以降の日時を指定する必要があります。

  * レスポンス(登録したWeb会議情報を返す)
    ```json
    {
      "id": <Web会議情報ID>
      "name": <Web会議名>,
      "startDateTime": <Web会議の開始日時>,
      "date": <Web会議の日付(UNIXエポックタイム)>
      "url": <Web会議のURL>,
      "registeredBy": <登録者>,
      "registeredAt": <登録日時>,
      "slackChannelId": <通知先のSlackチャンネル情報のID>
    }
    ```

* Web会議情報を検索
  ```js
  GET api/WebMeetings
  ```
  * クエリパラメータ
  
    |項目|値|備考|
    |:--|:--|:--|
    |ids|Web会議情報ID|複数指定時はカンマ区切りで指定|
    |fromDate|Web会議の日付範囲の開始日（ISO8601形式の文字列）|Web会議の開始日と終了日を指定する場合、終了日を含む過去日を指定
    |toDate|Web会議の日付範囲の終了日（ISO8601形式の文字列）|Web会議の開始日と終了日を指定する場合、開始日を含む未来日を指定
    |registeredBy|登録者|完全一致
    |slackChannelId|通知先のSlackチャンネル情報ID|

  * レスポンス
    ```json
    [{
      "id": <Web会議情報ID>
      "name": <Web会議名>,
      "startDateTime": <Web会議の開始日時>,
      "date": <Web会議の日付(UNIXエポックタイム)>
      "url": <Web会議のURL>,
      "registeredBy": <登録者>,
      "registeredAt": <登録日時>,
      "slackChannelId": <通知先のSlackチャンネル情報のID>
    }]
    ```

* Web会議情報を取得
  ```json
  GET api/WebMeetings/{Web会議情報ID(複数指定時はカンマ区切りで指定)}
  ```

  * レスポンス
    ```json
    [{
      "id": <Web会議情報ID>
      "name": <Web会議名>,
      "startDateTime": <Web会議の開始日時>,
      "date": <Web会議の日付(UNIXエポックタイム)>
      "url": <Web会議のURL>,
      "registeredBy": <登録者>,
      "registeredAt": <登録日時>,
      "slackChannelId": <通知先のSlackチャンネル情報のID>
    }]
    ```

* Web会議情報を削除
  ```json
  DELETE api/WebMeetings/{Web会議情報ID(複数指定時はカンマ区切りで指定)}
  ```

  * レスポンス(削除したWeb会議情報を返す)
    ```json
    [{
      "id": <Web会議情報ID>
      "name": <Web会議名>,
      "startDateTime": <Web会議の開始日時>,
      "date": <Web会議の日付(UNIXエポックタイム)>
      "url": <Web会議のURL>,
      "registeredBy": <登録者>,
      "registeredAt": <登録日時>,
      "slackChannelId": <通知先のSlackチャンネル情報のID>
    }]
    ```

####  通知先のSlackチャンネル情報を登録・検索・削除する REST API
* 通知先のSlackチャンネル情報を登録
  ```json
  POST api/SlackChannels
  {
      "name": <Slackチャンネル情報名>,
      "webhookUrl": <SlackチャンネルのWebhook URL>,
      "registeredBy": <登録者>
  }
  ```
  * 全ての項目が必須項目となります。

  * レスポンス(登録した通知先のSlackチャンネル情報を返す)
    ```json
    {
      "id": <Slackチャンネル情報ID>
      "name": <Slackチャンネル情報名>,
      "webhookUrl": <SlackチャンネルのWebhook URL>,
      "registeredBy": <登録者>,
      "registeredAt": <登録日時>
    }
    ```

* 通知先のSlackチャンネル情報を検索
  ```js
  GET api/SlackChannels
  ```
  * クエリパラメータ
  
    |項目|値|備考|
    |:--|:--|:--|
    |ids|Slackチャンネル情報ID|複数指定時はカンマ区切りで指定|
    |name|Slackチャンネル情報名|部分一致
    |webhookUrl|SlackチャンネルのWebhook URL|完全一致
    |registeredBy|登録者|完全一致

  * レスポンス
    ```json
    [{
      "id": <Slackチャンネル情報ID>
      "name": <Slackチャンネル情報名>,
      "webhookUrl": <SlackチャンネルのWebhook URL>,
      "registeredBy": <登録者>,
      "registeredAt": <登録日時>
    }]
    ```

* 通知先のSlackチャンネル情報を取得
  ```json
  GET api/SlackChannels/{Slackチャンネル情報ID(複数指定時はカンマ区切りで指定)}
  ```

  * レスポンス
    ```json
    [{
      "id": <Slackチャンネル情報ID>
      "name": <Slackチャンネル情報名>,
      "webhookUrl": <SlackチャンネルのWebhook URL>,
      "registeredBy": <登録者>,
      "registeredAt": <登録日時>
    }]
    ```

* 通知先のSlackチャンネル情報を削除
  ```json
  DELETE api/SlackChannels/{Slackチャンネル情報ID(複数指定時はカンマ区切りで指定)}
  ```

  * レスポンス(削除したWeb会議情報を返す)
    ```json
    [{
      "id": <Slackチャンネル情報ID>
      "name": <Slackチャンネル情報名>,
      "webhookUrl": <SlackチャンネルのWebhook URL>,
      "registeredBy": <登録者>,
      "registeredAt": <登録日時>
    }]
    ```
#### 当日のWeb会議情報を指定の Slack チャンネルに通知する定期バッチ
* 平日の朝9時に実行
  * 日本時間で動作させるために Azure Functions のアプリケーション設定に以下を追加しておく。

    |名前|値|
    |:--|:--|
    |WEBSITE_TIME_ZONE|Tokyo Standard Time|
    * 参考
      * https://docs.microsoft.com/ja-jp/azure/azure-functions/functions-bindings-timer?tabs=csharp#ncrontab-examples
* 翌日のWeb会議情報をWeb会議情報に指定されているSlackチャンネル情報ごとに開始時刻順にソートし、Slackチャンネルに通知します。
* Slackチャンネルに通知したWeb会議情報は削除します。
## 利用方法
### 環境
Azure Functions と Azure Cosmos DB を利用するため、 Azure のアカウントが必要です。
* ビルド環境
  * .NET Core 3.1 SDK
    * https://dotnet.microsoft.com/en-us/download/dotnet/3.1
  * Azure Functions Core Tools バージョン 3.x
    * https://docs.microsoft.com/ja-jp/azure/azure-functions/functions-run-local?tabs=v3%2Cwindows%2Ccsharp%2Cportal%2Cbash#install-the-azure-functions-core-tools
  * Visual Studio Code
    * https://code.visualstudio.com/
  * Visual Studio Code 用の C# 拡張機能
    * https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp
  * Visual Studio Code 用 Azure Functions 拡張機能
    * https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions
  * Visual Studio Code 用の Azure データベース拡張機能
    * https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-cosmosdb



### Install
Nuget: DensoCreate.XXX

```
C:\Project> NuGet Install DensoCreate.XXX
```

### 例

#### XXする
```cs
using DensoCreate.XXX;

// xxする
var xxx = new xxx();

```

#### YYする
```cs
// yyする
var yy = new xxx();

```
## サンプル
（サンプルへのリンクを記載する）
`samples/...` を参照のこと。

## 公開パッケージ
* DensoCreate.ProjectName
 
## 依存パッケージ
（複数のパッケージを公開する場合はパッケージごとに記載のこと）

### DensoCreate.XXX
* DensoCreate.Logging

### DensoCreate.YY
* なし

## フレームワーク
（複数のパッケージを公開する場合はパッケージごとに記載のこと）

### DensoCreate.XXX
* .NET Standard 2.0

### DensoCreate.YY
* WPF Core 3.1


## （参考リポジトリ）
ここのreadmeを参考にする。
* https://github.com/miles-team/DensoCreate.EventAggregator
* https://github.com/denso-create/LightningReview-ReviewFile

