# ESEP Assignment 8: Event-Driven Architecture

Opening an issue in this public repository triggers a GitHub webhook, which calls API Gateway, invokes an AWS Lambda function, and posts the issue link to Slack.

## Function

The function uses .NET 10 and the built-in System.Text.Json library. It accepts GitHub's JSON directly, reads `issue.html_url`, and sends `Issue Created: <link>` to Slack. Events without an issue, including GitHub's initial ping, return `ignored`. Issue updates also send a message, as allowed by the assignment.

The Slack URL is read exclusively from the Lambda environment variable `SLACK_URL`. Never commit its value or AWS credentials. Slack failures cause the invocation to fail instead of reporting a successful delivery.

## AWS configuration

- Region: `us-east-2` (Ohio).
- Lambda function: `EsepWebhook`.
- Runtime: `dotnet10`.
- Handler: `EsepWebhook::EsepWebhook.Function::FunctionHandler`.
- Execution role policy: `AWSLambdaBasicExecutionRole`.
- Environment variable: `SLACK_URL`, configured only in the Lambda console.
- API Gateway: Regional REST API, POST on the root resource, Lambda proxy integration **off**.
- Deployment stage: `default`.

## Deploy

From the repository root:

```powershell
git pull --ff-only origin main
cd EsepWebhook
dotnet lambda deploy-function EsepWebhook --region us-east-2
```

Confirm `SLACK_URL` is still configured after deployment.

## GitHub webhook

In repository Settings > Webhooks, set the payload URL to the API Gateway Invoke URL ending in `/default`, content type to `application/json`, and select only **Issues**. Keep SSL verification and Active enabled. Leave Secret empty for this assignment.

## Test

In the Lambda console, create a test event with:

```json
{
  "issue": {
    "html_url": "https://github.com/SMiqD/esep-webhooks/issues/1"
  }
}
```

A successful Slack delivery returns `ok`. This sample sends the supplied link; it does not create a GitHub issue.

For the complete integration test, open a real issue in this repository and confirm its link appears in the section's Slack channel. Check GitHub webhook Recent Deliveries and Lambda CloudWatch logs if needed.

The starter string test (`"test"`) no longer applies after deploying this handler.

## Submission

Submit the public repository URL after confirming the full GitHub-to-Slack flow works.
