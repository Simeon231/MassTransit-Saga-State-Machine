using Amazon.CDK;
using MasterDegreeDemo.CDK;

var app = new App();

new DemoStack(app, "DemoStack");

app.Synth();