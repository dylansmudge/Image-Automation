# PACE Ordering Jobs Azure Resource Manager Template

## Commands
Below are the commands to run to deploy to each environment. Prod is omitted on purpose.

### CONA Maint

```

Stage 1
az deployment group create --template-file ./deploy-image-automation-1-function.json --parameters "@./Parameters/cona-maint.parameters.json" --resource-group rg_nonpsub_m_pac_westus2 --subscription 5d26aadf-bc83-45db-908e-d9f69c2d27b9


```
