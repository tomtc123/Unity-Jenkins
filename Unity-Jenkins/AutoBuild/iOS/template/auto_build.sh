#!/bin/sh

cd ..
project_path="."

#Unity-iPhone不需要修改
scheme="Unity-iPhone"

#TeamID
development_team="REPLACE_TEAM_ID"

#描述文件的UUID
code_sign="REPLACE_UUID"

#描述文件的名称
sign_name="REPLACE_PROFILE_NAME"

#证书的名称（iPhone Distribution:  XXXXXXXXXX），可以在 Keychain Access 里面找到
code_sign_identity="REPLACE_DEVELOPER"

#AdHocExportOptions, AppStoreExportOptions分别需要改3个地方(bundle id, sign name, teamID)
AdHocExportOptions="./AutoXcodeBuild/AdHocExportOptions.plist"
AppStoreExportOptions="./AutoXcodeBuild/AppStoreExportOptions.plist"

currentTime=`date "+%Y%m%d%H%M%S"`
#ipa路径
exportIPAPath="./ios_ipa/${currentTime}"

echo "exportIPAPath=${exportIPAPath}"

#ssh 访问密钥串
security unlock-keychain "-p" "123456" "/Users/root/Library/Keychains/login.keychain"

autoBuild() {

	#打包之前先清理一下工程
	xcodebuild clean \
	-scheme ${scheme} \
	-configuration Release 
	if [[ $? != 0 ]]; then
		exit 1
	fi

	#开始编译工程 - 导出.xcarchive文件
	xcodebuild archive \
	-project "${project_path}/Unity-iPhone.xcodeproj" \
	-scheme ${scheme} \
	-configuration Release \
	-archivePath "./build/${scheme}.xcarchive" \
	CODE_SIGN_IDENTITY="${code_sign_identity}" \
	DEVELOPMENT_TEAM=${development_team} \
	PROVISIONING_PROFILE=${code_sign} \
	PROVISIONING_PROFILE_SPECIFIER=${sign_name}
	if [[ $? != 0 ]]; then
		exit 2
	fi

	#导出ipa包 ad-hoc
	xcodebuild -exportArchive \
	-archivePath "${project_path}/build/${scheme}.xcarchive" \
	-exportPath "${exportIPAPath}/adhoc" \
	-exportOptionsPlist ${AdHocExportOptions}
	if [[ $? != 0 ]]; then
		exit 3
	fi

	#导出ipa包 app store
	xcodebuild -exportArchive \
	-archivePath "${project_path}/build/${scheme}.xcarchive" \
	-exportPath "${exportIPAPath}/appstore" \
	-exportOptionsPlist ${AppStoreExportOptions}
	if [[ $? != 0 ]]; then
		exit 4
	fi
}
autoBuild

