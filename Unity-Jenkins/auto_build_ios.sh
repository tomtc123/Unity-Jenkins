#!/bin/sh

scheme="Unity-iPhone"
project_path="."
development_team="xxxxxx"
code_sign="xxxxx-xxx-xxx-xxxx-xxxxxx"
sign_name="mlkrdis"

autoBuild() {

	#打包之前先清理一下工程
	xcodebuild clean \
	-scheme ${scheme} \
	-configuration Release 
	if [[ $? != 0 ]]; then
		exit
	fi

	#开始编译工程 - 导出.xcarchive文件
	xcodebuild archive \
	-project "${project_path}/Unity-iPhone.xcodeproj" \
	-scheme ${scheme} \
	-configuration Release \
	-archivePath "./build/${scheme}.xcarchive" \
	CODE_SIGN_IDENTITY="iPhone Distribution: XING LU (TV2UDHQN3H)" \
	DEVELOPMENT_TEAM=${development_team} \
	PROVISIONING_PROFILE=${code_sign} \
	PROVISIONING_PROFILE_SPECIFIER=${sign_name}
	if [[ $? != 0 ]]; then
		exit
	fi

	#导出ipa包 ad-hoc
	xcodebuild -exportArchive \
	-archivePath "${project_path}/build/${scheme}.xcarchive" \
	-exportPath "~/Desktop/PublishIPA/${scheme}" \
	-exportOptionsPlist "./build/${scheme}/AdHocExportOptions.plist"
	if [[ $? != 0 ]]; then
		exit
	fi

	#导出ipa包 app store
	xcodebuild -exportArchive \
	-archivePath "${project_path}/build/${scheme}.xcarchive" \
	-exportPath "~/Desktop/PublishIPA/${scheme}" \
	-exportOptionsPlist "./build/${scheme}/AppStoreExportOptions.plist"
	if [[ $? != 0 ]]; then
		exit
	fi
}
autoBuild

