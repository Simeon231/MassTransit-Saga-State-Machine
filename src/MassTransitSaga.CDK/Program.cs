using Amazon.CDK;
using MassTransitSaga.CDK;

var app = new App();

_ = new DemoStack(app, "DemoStack");

app.Synth();